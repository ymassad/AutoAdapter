using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdaptationMethodProcessor
    {
        TypesToAddToModuleAndNewBodyForAdaptation ProcessAdaptationMethod(
            MethodDefinition adaptationMethod,
            MethodReferencesNeededForProcessingAdaptationMethod methodReferences);
    }
}