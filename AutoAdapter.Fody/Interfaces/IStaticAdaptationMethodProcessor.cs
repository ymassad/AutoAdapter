using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IStaticAdaptationMethodProcessor
    {
        TypesToAddToModuleAndNewBodyForAdaptation ProcessStaticAdaptationMethod(
            ModuleDefinition module,
            MethodDefinition adaptationMethod);
    }
}