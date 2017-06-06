using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class AdapterFactory : IAdapterFactory
    {
        private readonly ModuleDefinition moduleDefinition;

        public AdapterFactory(ModuleDefinition moduleDefinition)
        {
            this.moduleDefinition = moduleDefinition;
        }

        public TypeDefinition CreateAdapter(AdaptationRequestInstance request)
        {
            if (!request.DestinationType.Resolve().IsInterface)
                throw new Exception("The destination type must be an interface");

            var adapterType = new TypeDefinition(null , "Adapter" + Guid.NewGuid(), TypeAttributes.Public, moduleDefinition.TypeSystem.Object);

            var adaptedField = CreateAdaptedField(request);

            adapterType.Fields.Add(adaptedField);

            var extraParametersField = CreateExtraParametersField(request);

            if (request.ExtraParametersType.HasValue)
            {
                adapterType.Fields.Add(extraParametersField.GetValue());
            }

            adapterType.Interfaces.Add(new InterfaceImplementation(request.DestinationType));

            var constructor = CreateConstructor(request.SourceType, adaptedField, extraParametersField);

            adapterType.Methods.Add(constructor);

            var methods = CreateMethods(request, adaptedField, extraParametersField);

            adapterType.Methods.AddRange(methods);

            return adapterType;
        }

        private Maybe<FieldDefinition> CreateExtraParametersField(AdaptationRequestInstance request)
        {
            return request.ExtraParametersType
                .Chain(value =>
                    new FieldDefinition(
                        "extraParameters",
                        FieldAttributes.InitOnly | FieldAttributes.Private,
                        value));
        }

        private FieldDefinition CreateAdaptedField(AdaptationRequestInstance request)
        {
            return new FieldDefinition(
                "adapted",
                FieldAttributes.InitOnly | FieldAttributes.Private,
                request.SourceType);
        }

        private MethodDefinition[] CreateMethods(
            AdaptationRequestInstance request,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField)
        {
            var methods = new List<MethodDefinition>();

            var resolvedDestinationType = request.DestinationType.Resolve();

            var resolvedSourceType = request.SourceType.Resolve();
            
            foreach (var targetMethod in resolvedDestinationType.Methods)
            {
                var methodOnAdapter =
                    new MethodDefinition(
                        targetMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        targetMethod.ReturnType);

                foreach (var param in targetMethod.Parameters)
                {
                    var paramOnMethodOnAdapter = new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);

                    methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
                }

                var ilProcessor = methodOnAdapter.Body.GetILProcessor();

                ilProcessor.Emit(OpCodes.Ldarg_0);

                ilProcessor.Emit(OpCodes.Ldfld, adaptedField);

                var methodOnSourceType = resolvedSourceType.Methods.Single(x => x.Name == targetMethod.Name);

                var targetMethodParametersThatMatchSourceMethodParameters =
                    methodOnSourceType.Parameters
                        .Select(x => new SourceAndTargetParameters(x,
                            targetMethod.Parameters.FirstOrNoValue(p => p.Name == x.Name)))
                        .ToArray();

                targetMethodParametersThatMatchSourceMethodParameters
                    .ToList()
                    .ForEach(parameters =>
                    {
                        var instructions = CreateInstructionsForArgument(request, extraParametersField, parameters, ilProcessor);

                        ilProcessor.AppendRange(instructions);
                    });

                ilProcessor.Emit(OpCodes.Callvirt, methodOnSourceType);

                ilProcessor.Emit(OpCodes.Ret);

                methods.Add(methodOnAdapter);
            }

            return methods.ToArray();
        }

        private Instruction[] CreateInstructionsForArgument(
            AdaptationRequestInstance request,
            Maybe<FieldDefinition> extraParametersField,
            SourceAndTargetParameters parameters,
            ILProcessor ilProcessor)
        {
            if (parameters.TargetParameter.HasValue)
                return CreateInstructionsForArgumentUsingTargetParameter(
                    ilProcessor,
                    parameters);

            if (request.ExtraParametersType.HasValue)
            {
                return CreateInsturctionsForArgumentUsingExtraParametersObject(
                    ilProcessor,
                    request,
                    extraParametersField,
                    parameters);
            }

            if (parameters.SourceParameter.IsOptional &&
                parameters.SourceParameter.HasDefault &&
                parameters.SourceParameter.HasConstant)
            {
                return CreateInsturctionsArgumentUsingDefaultConstantValueOfSourceParameter(
                    ilProcessor,
                    parameters);
            }

            throw new Exception(
                $"Source parameter {parameters.SourceParameter.Name} is not optional with a default constant value and no extra parameters object is supplied");
        }

        private Instruction[] CreateInstructionsForArgumentUsingTargetParameter(
            ILProcessor ilProcessor,
            SourceAndTargetParameters parameters)
        {
            return new[]
            {
                ilProcessor.Create(OpCodes.Ldarg, parameters.TargetParameter.GetValue().Index + 1)
            };
        }

        private Instruction[] CreateInsturctionsArgumentUsingDefaultConstantValueOfSourceParameter(
            ILProcessor ilProcessor,
            SourceAndTargetParameters parameters)
        {
            switch (parameters.SourceParameter.ParameterType.FullName)
            {
                case "System.Int32":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_I4,
                            (int) parameters.SourceParameter.Constant)
                    };
                case "System.Int64":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_I8,
                            (long) parameters.SourceParameter.Constant)
                    };
                case "System.String":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldstr,
                            (string) parameters.SourceParameter.Constant)
                    };
                case "System.Single":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_R4,
                            (float) parameters.SourceParameter.Constant)
                    };
                case "System.Double":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_R8,
                            (double) parameters.SourceParameter.Constant)
                    };
            }
            throw new Exception("Unsupported optional parameter constant type");
        }

        private Instruction[] CreateInsturctionsForArgumentUsingExtraParametersObject(
            ILProcessor ilProcessor,
            AdaptationRequestInstance request,
            Maybe<FieldDefinition> extraParametersField,
            SourceAndTargetParameters parameters)
        {
            var resolvedExtraParametersType = request.ExtraParametersType.Chain(x => x.Resolve());

            var instructions = new List<Instruction>();

            instructions.Add(ilProcessor.Create(OpCodes.Ldarg_0));

            instructions.Add(ilProcessor.Create(OpCodes.Ldfld, extraParametersField.GetValue()));

            var propertyOnExtraParametersObject =
                resolvedExtraParametersType.GetValue().Properties
                    .FirstOrDefault(p => p.Name == parameters.SourceParameter.Name);

            if (propertyOnExtraParametersObject == null)
                throw new Exception(
                    $"Could not find property {parameters.SourceParameter.Name} on the extra parameters object");

            var propertyGetMethod = propertyOnExtraParametersObject.GetMethod;

            var extraParametersType = request.ExtraParametersType.GetValue();

            var propertyReturnType = propertyGetMethod.MethodReturnType.ReturnType;

            var propertyGetMethodReference =
                new MethodReference(
                        propertyGetMethod.Name,
                        propertyReturnType,
                        extraParametersType)
                    {HasThis = true};

            instructions.Add(ilProcessor.Create(OpCodes.Callvirt, propertyGetMethodReference));

            return instructions.ToArray();
        }

        private MethodDefinition CreateConstructor(
            TypeReference sourceType,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    moduleDefinition.TypeSystem.Void);

            constructor.Parameters.Add(new ParameterDefinition("adapted", ParameterAttributes.None, sourceType));

            extraParametersField.ExecuteIfHasValue(value =>
            {
                constructor.Parameters.Add(
                    new ParameterDefinition("extraParameters",
                    ParameterAttributes.None,
                    value.FieldType));
            });

            var objectConstructor =
                moduleDefinition.ImportReference(moduleDefinition.TypeSystem.Object.Resolve().GetConstructors().First());

            var processor = constructor.Body.GetILProcessor();

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, objectConstructor);

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Stfld, adaptedField);

            extraParametersField.ExecuteIfHasValue(value =>
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_2);
                processor.Emit(OpCodes.Stfld, value);
            });

            processor.Emit(OpCodes.Ret);

            return constructor;
        }
    }
}