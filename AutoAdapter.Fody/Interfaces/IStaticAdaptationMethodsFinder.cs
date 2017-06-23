using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IStaticAdaptationMethodsFinder
    {
        MethodDefinition[] FindStaticAdaptationMethods(ModuleDefinition moduleDefinition);
    }
}