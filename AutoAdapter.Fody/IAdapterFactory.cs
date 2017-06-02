using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public interface IAdapterFactory
    {
        TypeDefinition CreateAdapter(AdaptationRequestInstance request);
    }
}