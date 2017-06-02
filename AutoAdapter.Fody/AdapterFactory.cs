using System;
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

            var adaptedField = AddAdaptedField(request, adapterType);

            var extraParametersField = AddExtraParametersFieldIfRequired(request, adapterType);

            adapterType.Interfaces.Add(new InterfaceImplementation(request.DestinationType));

            AddConstructor(request.SourceType, adaptedField, extraParametersField, adapterType);

            AddMethods(request, adaptedField, extraParametersField, adapterType);

            return adapterType;
        }

        private Maybe<FieldDefinition> AddExtraParametersFieldIfRequired(AdaptationRequestInstance request, TypeDefinition adapterType)
        {
            Maybe<FieldDefinition> extraParametersField = Maybe<FieldDefinition>.NoValue();

            if (request.ExtraParametersType.HasValue)
            {
                extraParametersField =
                    new FieldDefinition(
                        "extraParameters",
                        FieldAttributes.InitOnly | FieldAttributes.Private,
                        request.ExtraParametersType.GetValue());

                adapterType.Fields.Add(extraParametersField.GetValue());
            }
            return extraParametersField;
        }

        private FieldDefinition AddAdaptedField(AdaptationRequestInstance request, TypeDefinition adapterType)
        {
            var adaptedField =
                new FieldDefinition(
                    "adapted",
                    FieldAttributes.InitOnly | FieldAttributes.Private,
                    request.SourceType);

            adapterType.Fields.Add(adaptedField);

            return adaptedField;
        }

        private void AddMethods(
            AdaptationRequestInstance request,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField,
            TypeDefinition adapterType)
        {
            var resolvedDestinationType = request.DestinationType.Resolve();

            var resolvedSourceType = request.SourceType.Resolve();

            var resolvedExtraParametersType = request.ExtraParametersType.Chain(x => x.Resolve());

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

                var methodOnAdapterIlProcessor = methodOnAdapter.Body.GetILProcessor();

                methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg_0);

                methodOnAdapterIlProcessor.Emit(OpCodes.Ldfld, adaptedField);

                var methodOnSourceType = resolvedSourceType.Methods.Single(x => x.Name == targetMethod.Name);

                var targetMethodParametersThatMatchSourceMethodParameters =
                    methodOnSourceType.Parameters
                        .Select(x => new SourceAndTargetParameters(x, targetMethod.Parameters.FirstOrNoValue(p => p.Name == x.Name)))
                        .ToArray();

                targetMethodParametersThatMatchSourceMethodParameters
                    .ToList()
                    .ForEach(parameters =>
                    {
                        if (parameters.TargetParameter.HasNoValue)
                        {
                            EmitArgumentUsingExtraParametersObject(
                                request,
                                extraParametersField,
                                methodOnAdapterIlProcessor,
                                resolvedExtraParametersType,
                                parameters);
                        }
                        else
                        {
                            methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg, parameters.SourceParameter.Index + 1);
                        } 
                    });

                methodOnAdapterIlProcessor.Emit(OpCodes.Callvirt, methodOnSourceType);

                methodOnAdapterIlProcessor.Emit(OpCodes.Ret);

                adapterType.Methods.Add(methodOnAdapter);
            }
        }

        private void EmitArgumentUsingExtraParametersObject(
            AdaptationRequestInstance request,
            Maybe<FieldDefinition> extraParametersField,
            ILProcessor methodOnAdapterIlProcessor,
            Maybe<TypeDefinition> resolvedExtraParametersType,
            SourceAndTargetParameters parameters)
        {
            if (request.ExtraParametersType.HasNoValue)
                throw new Exception("Expected ExtraParametersType to have a value");

            methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg_0);

            methodOnAdapterIlProcessor.Emit(OpCodes.Ldfld, extraParametersField.GetValue());

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

            methodOnAdapterIlProcessor.Emit(OpCodes.Callvirt, propertyGetMethodReference);
        }

        private void AddConstructor(
            TypeReference sourceType,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField,
            TypeDefinition adapterType)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    moduleDefinition.TypeSystem.Void);

            constructor.Parameters.Add(new ParameterDefinition("adapted", ParameterAttributes.None, sourceType));

            if (extraParametersField.HasValue)
            {
                constructor.Parameters.Add(new ParameterDefinition("extraParameters", ParameterAttributes.None, extraParametersField.GetValue().FieldType));
            }

            var objectConstructor =
                moduleDefinition.ImportReference(moduleDefinition.TypeSystem.Object.Resolve().GetConstructors().First());

            var processor = constructor.Body.GetILProcessor();

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, objectConstructor);

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Stfld, adaptedField);

            if (extraParametersField.HasValue)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_2);
                processor.Emit(OpCodes.Stfld, extraParametersField.GetValue());
            }

            processor.Emit(OpCodes.Ret);
            adapterType.Methods.Add(constructor);
        }
    }
}