using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdaptationMethodProcessor<TAdaptationMethodKind>
    {
        TypesToAddToModuleAndNewBodyForAdaptation ProcessAdaptationMethod(
            ModuleDefinition module,
            TAdaptationMethodKind adaptationMethod);
    }
}