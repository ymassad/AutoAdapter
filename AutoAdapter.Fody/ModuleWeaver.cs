using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class ModuleWeaver
    {
        public Action<string> LogInfo { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        public ModuleWeaver()
        {
            LogInfo = m => { };
        }

        private IAdaptationMethodsFinder CreateAdaptationMethodsFinder() => new AdaptationMethodsFinder();

        private IAdaptationMethodProcessor CreateAdaptationMethodProcessor() =>
            new AdaptationMethodProcessor(
                new AdapterFactory(
                    ModuleDefinition,
                    new CreatorOfInsturctionsForArgument(),
                    new SourceAndTargetMethodsMapper()),
                new AdaptationRequestsFinder());

        public void Execute()
        {
            var adaptationMethodProcessor = CreateAdaptationMethodProcessor();

            var adaptationMethodsFinder = CreateAdaptationMethodsFinder();

            var adaptationMethods = adaptationMethodsFinder.FindAdaptationMethods(ModuleDefinition);

            var methodReferencesNeededForProcessingAdaptationMethod =
                ImportMethodReferencesNeededForProcessingAdaptationMethods(ModuleDefinition);

            foreach (var adaptationMethod in adaptationMethods)
            {
                var changes =
                    adaptationMethodProcessor
                        .ProcessAdaptationMethod(
                            adaptationMethod,
                            methodReferencesNeededForProcessingAdaptationMethod);

                ModuleDefinition.Types.AddRange(changes.TypesToAdd);

                adaptationMethod.Body.Instructions.Clear();

                var ilProcessor = adaptationMethod.Body.GetILProcessor();

                ilProcessor.AppendRange(changes.NewBodyForAdaptationMethod);
            }
        }

        private MethodReferencesNeededForProcessingAdaptationMethod ImportMethodReferencesNeededForProcessingAdaptationMethods(
            ModuleDefinition module)
        {
            ModuleDefinition mscorlib = ModuleDefinition.ReadModule(typeof(object).Module.FullyQualifiedName);

            var typeDefinition = mscorlib.GetType("System.Type");

            var exceptionDefinition = mscorlib.GetType("System.Exception");

            var getTypeFromHandleMethod =
                module
                    .ImportReference(typeDefinition.Methods.First(x => x.Name == "GetTypeFromHandle"));

            var equalsMethod =
                module
                    .ImportReference(
                        typeDefinition.Methods
                            .First(
                                x => x.Name == "Equals"
                                     && x.Parameters.Any()
                                     && x.Parameters[0].ParameterType.FullName == "System.Type"));

            var exceptionConstructor =
                module.ImportReference(exceptionDefinition.GetConstructors()
                    .First(x => x.Parameters.Count == 1 && x.Parameters[0].ParameterType.FullName == "System.String"));

            var getTypeMethod = module
                .ImportReference(
                    mscorlib.GetType("System.Object")
                        .Methods.Single(x => x.Name == "GetType" && x.Parameters.Count == 0));

            return new MethodReferencesNeededForProcessingAdaptationMethod(
                getTypeFromHandleMethod, equalsMethod, getTypeMethod, exceptionConstructor);
        }

    }
}
