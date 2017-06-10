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
            var getTypeFromHandleMethod =
                module.ImportReference(
                    typeof(Type).GetMethod("GetTypeFromHandle"));

            var equalsMethod =
                module.ImportReference(
                    typeof(Type).GetMethod("Equals", new[] {typeof(Type)}));

            var exceptionConstructor =
                module.ImportReference(
                    typeof(Exception).GetConstructor(new[] {typeof(string)}));

            var getTypeMethod =
                module.ImportReference(
                    typeof(object).GetMethod("GetType"));

            var objectConstructor =
                module.ImportReference(
                    typeof(object).GetConstructor(new Type[0]));

            return new MethodReferencesNeededForProcessingAdaptationMethod(
                getTypeFromHandleMethod,
                equalsMethod,
                getTypeMethod,
                exceptionConstructor,
                objectConstructor);
        }
    }
}
