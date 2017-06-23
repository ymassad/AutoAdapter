using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdaptationMethodsFinder<TAdaptationMethodKind>
    {
        TAdaptationMethodKind[] FindAdaptationMethods(ModuleDefinition moduleDefinition);
    }
}