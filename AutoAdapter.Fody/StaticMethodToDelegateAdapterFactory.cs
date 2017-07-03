using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodToDelegateAdapterFactory : IStaticMethodToInterfaceAdapterFactory<StaticMethodToDelegateAdaptationRequest>
    {
        private readonly IMembersCreatorForAdapterThatAdaptsFromStaticMethod membersCreator;

        private readonly IReferenceImporter referenceImporter;

        public StaticMethodToDelegateAdapterFactory(
            IMembersCreatorForAdapterThatAdaptsFromStaticMethod membersCreator,
            IReferenceImporter referenceImporter)
        {
            this.membersCreator = membersCreator;
            this.referenceImporter = referenceImporter;
        }

        public TypeDefinition CreateAdapter(
            ModuleDefinition module,
            StaticMethodToDelegateAdaptationRequest request)
        {
            var resolvedDestinationType = request.DestinationType.Resolve();

            if (!TypeUtilities.IsDelegateType(resolvedDestinationType))
                throw new Exception("The destination type must be delegate");

            var destinationMethods = resolvedDestinationType.GetMethods().ToArray();

            var destinationMethod = destinationMethods.Single(x => x.Name == "Invoke");

            var resolvedSourceClass = request.SourceStaticClass.Resolve();

            var sourceClassMethods = resolvedSourceClass.GetMethods().ToArray();

            var sourceMethodsWithRequestedName = sourceClassMethods
                .Where(x => x.Name.Equals(request.SourceStaticMethodName)).ToArray();

            if (sourceMethodsWithRequestedName.Length == 0)
                throw new Exception("Could not find requested method in source static class");

            var sourceMethod = sourceMethodsWithRequestedName.First();

            var membersToAdd =
                membersCreator.CreateMembers(
                    module,
                    new SourceAndTargetMethods(destinationMethod, sourceMethod),
                    request.ExtraParametersObjectType);

            var adapterType = new TypeDefinition(
                null, "Adapter" + Guid.NewGuid(), TypeAttributes.Public, referenceImporter.ImportObjectType(module));

            adapterType.Methods.AddRange(membersToAdd.MethodsToAdd);

            adapterType.Fields.AddRange(membersToAdd.FieldsToAdd);

            return adapterType;
        }
    }
}