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
