using System.Collections.Generic;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public class ModuleProcessor<TAdaptationMethodKind> : IModuleProcessor where TAdaptationMethodKind: AdaptationMethod
    {
        private readonly IAdaptationMethodsFinder<TAdaptationMethodKind> adaptationMethodsFinder;

        private readonly IAdaptationMethodProcessor<TAdaptationMethodKind> adaptationMethodProcessor;

        public ModuleProcessor(
            IAdaptationMethodsFinder<TAdaptationMethodKind> adaptationMethodsFinder,
            IAdaptationMethodProcessor<TAdaptationMethodKind> adaptationMethodProcessor)
        {
            this.adaptationMethodsFinder = adaptationMethodsFinder;
            this.adaptationMethodProcessor = adaptationMethodProcessor;
        }

        public ChangesToModule ProcessModule(ModuleDefinition module)
        {
            var adaptationMethods = adaptationMethodsFinder.FindAdaptationMethods(module);

            var typesToAdd = new List<TypeDefinition>();

            var newMethodBodies = new List<NewBodyForMethod>();

            foreach (var adaptationMethod in adaptationMethods)
            {
                var changes =
                    adaptationMethodProcessor
                        .ProcessAdaptationMethod(
                            module,
                            adaptationMethod);

                typesToAdd.AddRange(changes.TypesToAdd);

                newMethodBodies.Add(new NewBodyForMethod(adaptationMethod.Method , changes.NewBodyForAdaptationMethod));
            }

            return new ChangesToModule(typesToAdd.ToArray(), newMethodBodies.ToArray());
        }
    }
}