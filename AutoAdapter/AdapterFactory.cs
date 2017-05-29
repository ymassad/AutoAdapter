using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter
{
    public class AdapterFactory : IAdapterFactory
    {
        private readonly ModuleDefinition moduleDefinition;

        public AdapterFactory(ModuleDefinition moduleDefinition)
        {
            this.moduleDefinition = moduleDefinition;
        }

        public TypeDefinition CreateAdapter(TypeDefinition fromType, TypeDefinition toType)
        {
            if(!toType.IsInterface)
                throw new Exception("The destination type must be an interface");

            var adapterType = new TypeDefinition(null , "Adapter" + Guid.NewGuid(), TypeAttributes.Public, moduleDefinition.TypeSystem.Object);

            var adaptedField = new FieldDefinition("adapted", FieldAttributes.InitOnly | FieldAttributes.Private, fromType);

            adapterType.Fields.Add(adaptedField);

            adapterType.Interfaces.Add(new InterfaceImplementation(toType));

            CreateConstructor(fromType, adaptedField, adapterType);

            CreateMethods(fromType, toType, adaptedField, adapterType);

            return adapterType;
        }

        private static void CreateMethods(
            TypeDefinition fromType,
            TypeDefinition toType,
            FieldDefinition adaptedField,
            TypeDefinition adapterType)
        {
            foreach (var method in toType.Methods)
            {
                var methodOnAdapter =
                    new MethodDefinition(
                        method.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        method.ReturnType);

                foreach (var param in method.Parameters)
                {
                    var paramOnMethodOnAdapter = new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);

                    methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
                }

                var methodOnAdapterIlProcessor = methodOnAdapter.Body.GetILProcessor();

                methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg_0);

                methodOnAdapterIlProcessor.Emit(OpCodes.Ldfld, adaptedField);

                Enumerable.Range(1, method.Parameters.Count)
                    .ToList()
                    .ForEach(x => methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg, x));

                var methodOnAdaptedObject = fromType.Methods.Single(x => x.Name == method.Name);

                methodOnAdapterIlProcessor.Emit(OpCodes.Callvirt, methodOnAdaptedObject);

                methodOnAdapterIlProcessor.Emit(OpCodes.Ret);

                adapterType.Methods.Add(methodOnAdapter);
            }
        }

        private void CreateConstructor(TypeDefinition fromType, FieldDefinition field, TypeDefinition adapterType)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    moduleDefinition.TypeSystem.Void);

            constructor.Parameters.Add(new ParameterDefinition("adapted", ParameterAttributes.None, fromType));

            var objectConstructor =
                moduleDefinition.ImportReference(moduleDefinition.TypeSystem.Object.Resolve().GetConstructors().First());

            var processor = constructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, objectConstructor);

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Stfld, field);

            processor.Emit(OpCodes.Ret);
            adapterType.Methods.Add(constructor);
        }
    }
}