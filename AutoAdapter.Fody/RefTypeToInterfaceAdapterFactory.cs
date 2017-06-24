using System;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace AutoAdapter.Fody
{
    public class RefTypeToInterfaceAdapterFactory : IAdapterFactory
    {
        private readonly IAdapterMethodsCreator adapterMethodsCreator;
        private readonly IReferenceImporter referenceImporter;

        public RefTypeToInterfaceAdapterFactory(IAdapterMethodsCreator adapterMethodsCreator, IReferenceImporter referenceImporter)
        {
            this.adapterMethodsCreator = adapterMethodsCreator;
            this.referenceImporter = referenceImporter;
        }

        public TypeDefinition CreateAdapter(ModuleDefinition module, AdaptationRequestInstance request)
        {
            if (!request.DestinationType.Resolve().IsInterface)
                throw new Exception("The destination type must be an interface");
            
            var adapterType = new TypeDefinition(null , "Adapter" + Guid.NewGuid(), TypeAttributes.Public, referenceImporter.ImportObjectType(module));

            var adaptedField = CreateAdaptedField(request);

            adapterType.Fields.Add(adaptedField);

            var extraParametersField =
                request.ExtraParametersObjectType.Chain(ExtraParametersObjectUtilities.CreateExtraParametersField);

            extraParametersField.ExecuteIfHasValue(field => adapterType.Fields.Add(field));

            adapterType.Interfaces.Add(new InterfaceImplementation(request.DestinationType));

            var constructor = CreateConstructor(request.SourceType, adaptedField, extraParametersField, module);

            adapterType.Methods.Add(constructor);

            var methods = adapterMethodsCreator.CreateMethods(request, adaptedField, extraParametersField);

            adapterType.Methods.AddRange(methods);

            return adapterType;
        }

        private FieldDefinition CreateAdaptedField(AdaptationRequestInstance request)
        {
            return new FieldDefinition(
                "adapted",
                FieldAttributes.InitOnly | FieldAttributes.Private,
                request.SourceType);
        }

        private MethodDefinition CreateConstructor(
            TypeReference sourceType,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField,
            ModuleDefinition module)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    referenceImporter.ImportVoidType(module));

            constructor.Parameters.Add(new ParameterDefinition("adapted", ParameterAttributes.None, sourceType));

            extraParametersField.ExecuteIfHasValue(value =>
            {
                constructor.Parameters.Add(
                    new ParameterDefinition("extraParameters",
                    ParameterAttributes.None,
                    value.FieldType));
            });

            var processor = constructor.Body.GetILProcessor();

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, referenceImporter.ImportObjectConstructor(module));

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