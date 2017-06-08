using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdaptationMethodsFinder
    {
        MethodDefinition[] FindAdaptationMethods(ModuleDefinition moduleDefinition);
    }
}