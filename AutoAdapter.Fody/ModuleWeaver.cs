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

        public void Execute()
        {
            IModuleProcessor moduleProcessor =
                new ModuleProcessor(
                    new AdaptationMethodsFinder(),
                    new AdaptationMethodProcessor(
                        new AdapterFactory(
                            new CreatorOfInsturctionsForArgument(),
                            new SourceAndTargetMethodsMapper()),
                        new AdaptationRequestsFinder()));

            var methodReferencesNeededForProcessingAdaptationMethod =
                ImportMethodReferencesNeededForProcessingAdaptationMethods(ModuleDefinition);

            var moduleChanges =
                moduleProcessor
                    .ProcessModule(ModuleDefinition, methodReferencesNeededForProcessingAdaptationMethod);

            ModuleDefinition.Types.AddRange(moduleChanges.TypesToAdd);

            moduleChanges.NewMethodBodies.ToList().ForEach(method =>
            {
                method.Method.Body.Instructions.Clear();

                var ilProcessor = method.Method.Body.GetILProcessor();

                ilProcessor.AppendRange(method.NewBody);
            });
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

            var objectConstructor = module.ImportReference(
                module.TypeSystem.Object.Resolve().GetConstructors().First());

            return new MethodReferencesNeededForProcessingAdaptationMethod(
                getTypeFromHandleMethod,
                equalsMethod,
                getTypeMethod,
                exceptionConstructor,
                objectConstructor);
        }

    }
}
