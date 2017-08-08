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
                    CreateModuleProcessorForStaticMethodAdaptation());

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

        private ModuleProcessor<StaticMethodAdaptationMethod> CreateModuleProcessorForStaticMethodAdaptation()
        {
            var membersCreatorForAdapterThatAdaptsFromStaticMethod =
                new MembersCreatorForAdapterThatAdaptsFromStaticMethod(
                    new CreatorOfInsturctionsForArgument(),
                    new ReferenceImporter());

            return new ModuleProcessor<StaticMethodAdaptationMethod>(
                new StaticMethodAdaptationMethodsFinder(),
                new StaticMethodAdaptationMethodProcessor(
                    new StaticMethodToInterfaceAdapterFactory(
                        membersCreatorForAdapterThatAdaptsFromStaticMethod, 
                        new ReferenceImporter()),
                    new StaticMethodMethodAdaptationRequestsFinder<StaticMethodToInterfaceAdaptationRequest>(),
                    new StaticMethodToDelegateAdapterFactory(
                        membersCreatorForAdapterThatAdaptsFromStaticMethod,
                        new ReferenceImporter()),
                    new StaticMethodMethodAdaptationRequestsFinder<StaticMethodToDelegateAdaptationRequest>(),
                    new ReferenceImporter()));
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
                    new RefTypeToInterfaceAdaptationRequestsFinder(),
                    new ReferenceImporter()));
        }
    }
}
