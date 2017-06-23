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
                            new AdapterMethodsCreator( 
                                new CreatorOfInsturctionsForArgument(),
                                new SourceAndTargetMethodsMapper()),
                            new ReferenceImporter()),
                        new AdaptationRequestsFinder(),
                        new ReferenceImporter()));

            var staticMethodAdaptationMethodsFinder = new StaticMethodAdaptationMethodsFinder();

            var methods = staticMethodAdaptationMethodsFinder.FindStaticAdaptationMethods(ModuleDefinition);

            var requestsFinder = new StaticMethodAdaptationRequestsFinder();

            StaticMethodAdapterFactory factory = new StaticMethodAdapterFactory(new ReferenceImporter());

            var processor = new StaticAdaptationMethodProcessor(factory, requestsFinder, new ReferenceImporter());

            foreach (var method in methods )
            {
                var result =  processor.ProcessStaticAdaptationMethod(ModuleDefinition, method);

                method.Body.Instructions.Clear();

                method.Body.Instructions.AddRange(result.NewBodyForAdaptationMethod);

                ModuleDefinition.Types.AddRange(result.TypesToAdd);
            }

            var moduleChanges =
                moduleProcessor
                    .ProcessModule(ModuleDefinition);

            ModuleDefinition.Types.AddRange(moduleChanges.TypesToAdd);

            moduleChanges.NewMethodBodies.ToList().ForEach(method =>
            {
                method.Method.Body.Instructions.Clear();

                var ilProcessor = method.Method.Body.GetILProcessor();

                ilProcessor.AppendRange(method.NewBody);
            });
        }
    }
}
