using System;
using System.Collections.Generic;
using System.Linq;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class AdapterFactory : IAdapterFactory
    {
        private readonly ModuleDefinition moduleDefinition;
        private readonly ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument;

        public AdapterFactory(
            ModuleDefinition moduleDefinition,
            ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument)
        {
            this.moduleDefinition = moduleDefinition;
            this.creatorOfInsturctionsForArgument = creatorOfInsturctionsForArgument;
        }

        public TypeDefinition CreateAdapter(AdaptationRequestInstance request)
        {
            if (!request.DestinationType.Resolve().IsInterface)
                throw new Exception("The destination type must be an interface");

            var adapterType = new TypeDefinition(null , "Adapter" + Guid.NewGuid(), TypeAttributes.Public, moduleDefinition.TypeSystem.Object);

            var adaptedField = CreateAdaptedField(request);

            adapterType.Fields.Add(adaptedField);

            var extraParametersField = CreateExtraParametersField(request);

            if (request.ExtraParametersObjectType.HasValue)
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
            return request.ExtraParametersObjectType
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
                        var instructions =
                            creatorOfInsturctionsForArgument
                                .CreateInstructionsForArgument(
                                    parameters,
                                    ilProcessor,
                                    request.ExtraParametersObjectType,
                                    extraParametersField);

                        ilProcessor.AppendRange(instructions);
                    });

                ilProcessor.Emit(OpCodes.Callvirt, methodOnSourceType);

                ilProcessor.Emit(OpCodes.Ret);

                methods.Add(methodOnAdapter);
            }

            return methods.ToArray();
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