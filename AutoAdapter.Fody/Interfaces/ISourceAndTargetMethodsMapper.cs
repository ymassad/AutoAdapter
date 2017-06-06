using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface ISourceAndTargetMethodsMapper
    {
        SourceAndTargetMethods[] CreateMap(
            TypeDefinition resolvedDestinationType,
            TypeDefinition resolvedSourceType);
    }
}