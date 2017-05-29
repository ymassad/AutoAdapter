using Mono.Cecil;

namespace AutoAdapter
{
    public interface IAdapterFactory
    {
        TypeDefinition CreateAdapter(TypeDefinition fromType, TypeDefinition toType);
    }
}