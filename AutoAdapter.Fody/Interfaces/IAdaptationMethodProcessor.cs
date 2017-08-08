using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdaptationMethodProcessor<TAdaptationMethodKind>
    {
        TypesToAddToModuleAndNewBodyForAdaptationMethod ProcessAdaptationMethod(
            ModuleDefinition module,
            TAdaptationMethodKind adaptationMethod);
    }
}