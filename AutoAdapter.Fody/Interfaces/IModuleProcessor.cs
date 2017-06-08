using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IModuleProcessor
    {
        ChangesToModule ProcessModule(ModuleDefinition moduleDefinition, MethodReferencesNeededForProcessingAdaptationMethod neededReferences);
    }
}