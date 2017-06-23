using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

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
            var moduleProcessor =
                new CompositeModuleProcessor(
                    CreateModuleProcessorForRefTypeToInterfaceAdaptation(),
                    CreateModuleProcessorForStaticMethodToInterfaceAdaptation());

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

        private ModuleProcessor<StaticMethodToInterfaceAdaptationMethod> CreateModuleProcessorForStaticMethodToInterfaceAdaptation()
        {
            return new ModuleProcessor<StaticMethodToInterfaceAdaptationMethod>(
                new StaticMethodToInterfaceAdaptationMethodsFinder(),
                new StaticMethodToInterfaceAdaptationMethodProcessor(
                    new StaticMethodToInterfaceAdapterFactory(
                        new ReferenceImporter(),
                        new CreatorOfInsturctionsForArgument()),
                    new StaticMethodToInterfaceMethodAdaptationRequestsFinder(), new ReferenceImporter()));
        }

        private ModuleProcessor<RefTypeToInterfaceAdaptationMethod> CreateModuleProcessorForRefTypeToInterfaceAdaptation()
        {
            return new ModuleProcessor<RefTypeToInterfaceAdaptationMethod>(
                new RefTypeToInterfaceAdaptationMethodsFinder(),
                new RefTypeToInterfaceAdaptationMethodProcessor(
                    new RefTypeToInterfaceAdapterFactory(
                        new AdapterMethodsCreator( 
                            new CreatorOfInsturctionsForArgument(),
                            new SourceAndTargetMethodsMapper()),
                        new ReferenceImporter()),
                    new AdaptationRequestsFinder(),
                    new ReferenceImporter()));
        }
    }
}
