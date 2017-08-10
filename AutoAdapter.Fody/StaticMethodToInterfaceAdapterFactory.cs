using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodToInterfaceAdapterFactory : IStaticMethodToInterfaceAdapterFactory<StaticMethodToInterfaceAdaptationRequest>
    {
        private readonly IMembersCreatorForAdapterThatAdaptsFromStaticMethod membersCreator;

        private readonly IReferenceImporter referenceImporter;

        public StaticMethodToInterfaceAdapterFactory(
            IMembersCreatorForAdapterThatAdaptsFromStaticMethod membersCreator,
            IReferenceImporter referenceImporter)
        {
            this.membersCreator = membersCreator;
            this.referenceImporter = referenceImporter;
        }

        public TypeDefinition CreateAdapter(
            ModuleDefinition module,
            StaticMethodToInterfaceAdaptationRequest request)
        {
            var resolvedDestinationType = request.DestinationType.Resolve();

            if (!resolvedDestinationType.IsInterface)
                throw new Exception("The destination type must be an interface");

            var destinationMethods = resolvedDestinationType.GetMethods().ToArray();
            
            if(destinationMethods.Length != 1)
                throw new Exception("Target interface should contain only one method");

            var destinationMethod = destinationMethods[0];

            var resolvedSourceClass = request.SourceStaticClass.Resolve();

            var sourceClassMethods = resolvedSourceClass.GetMethods().ToArray();

            var sourceMethodsWithRequestedName = sourceClassMethods
                .Where(x => x.Name.Equals(request.SourceStaticMethodName)).ToArray();

            if(sourceMethodsWithRequestedName.Length == 0)
                throw new Exception("Could not find requested method in source static class");

            var sourceMethod = sourceMethodsWithRequestedName.First();

            var membersToAdd =
                membersCreator.CreateMembers(
                    module,
                    new SourceAndTargetMethods(new MethodOnAReferencedType(request.SourceStaticClass, sourceMethod), new MethodOnAReferencedType(request.DestinationType, destinationMethod)),
                    request.ExtraParametersObjectType);

            var adapterType = new TypeDefinition(
                null, "Adapter" + Guid.NewGuid(), TypeAttributes.Public, referenceImporter.ImportObjectType(module));

            adapterType.Methods.AddRange(membersToAdd.MethodsToAdd);

            adapterType.Fields.AddRange(membersToAdd.FieldsToAdd);

            adapterType.Interfaces.Add(new InterfaceImplementation(request.DestinationType));

            return adapterType;
        }
    }
}
