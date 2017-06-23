using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodToInterfaceAdapterFactory : IStaticMethodAdapterFactory
    {
        private readonly IReferenceImporter referenceImporter;

        public StaticMethodToInterfaceAdapterFactory(IReferenceImporter referenceImporter)
        {
            this.referenceImporter = referenceImporter;
        }

        public TypeDefinition CreateAdapter(
            ModuleDefinition module,
            StaticMethodAdaptationRequestInstance request)
        {
            var resolvedDestinationType = request.DestinationType.Resolve();

            if (!resolvedDestinationType.IsInterface)
                throw new Exception("The destination type must be an interface");

            var destinationMethods = resolvedDestinationType.GetMethods().ToArray();
            
            if(destinationMethods.Length != 1)
                throw new Exception("Target interface should contain only one method");

            var resolvedSourceClass = request.SourceStaticClass.Resolve();

            var sourceClassMethods = resolvedSourceClass.GetMethods().ToArray();

            var sourceMethodsWithRequestedName = sourceClassMethods
                .Where(x => x.Name.Equals(request.SourceStaticMethodName)).ToArray();

            if(sourceMethodsWithRequestedName.Length == 0)
                throw new Exception("Could not find requested method in source static class");

            var sourceMethod = sourceMethodsWithRequestedName.First();

            var destinationMethod = destinationMethods[0];

            if (sourceMethod.Parameters.Count != destinationMethod.Parameters.Count)
                throw new Exception("Target method and source method parameter count are different");

            if (!sourceMethod.Parameters
                .Select(x => x.ParameterType.FullName)
                .SequenceEqual(
                    destinationMethod.Parameters.Select(x => x.ParameterType.FullName)))
            {
                throw new Exception("Target and source parameter types are different");
            }

            var adapterType = new TypeDefinition(
                null, "Adapter" + Guid.NewGuid(), TypeAttributes.Public, ImportObjectType(module));

            adapterType.Interfaces.Add(new InterfaceImplementation(request.DestinationType));
            
            var methodOnAdapter =
                new MethodDefinition(
                    destinationMethod.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    destinationMethod.ReturnType);

            foreach (var param in destinationMethod.Parameters)
            {
                var paramOnMethodOnAdapter =
                    new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);

                methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
            }

            var ilProcessor = methodOnAdapter.Body.GetILProcessor();

            foreach (var param in destinationMethod.Parameters)
            {
                ilProcessor.Emit(OpCodes.Ldarg, param.Index + 1);
            }
            
            ilProcessor.Emit(OpCodes.Call, sourceMethod);

            ilProcessor.Emit(OpCodes.Ret);

            adapterType.Methods.Add(methodOnAdapter);

            var constructor = CreateConstructor(module);

            adapterType.Methods.Add(constructor);

            return adapterType;
        }

        private MethodDefinition CreateConstructor(ModuleDefinition module)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    ImportVoidType(module));

            var processor = constructor.Body.GetILProcessor();

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, ImportObjectConstructor(module));

            processor.Emit(OpCodes.Ret);

            return constructor;
        }

        private TypeReference ImportVoidType(ModuleDefinition module)
        {
            return referenceImporter.ImportTypeReference(module, typeof(void));
        }

        private TypeReference ImportObjectType(ModuleDefinition module)
        {
            return referenceImporter.ImportTypeReference(module, typeof(object));
        }

        private MethodReference ImportObjectConstructor(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(object).GetConstructor(new Type[0]));
        }
    }
}
