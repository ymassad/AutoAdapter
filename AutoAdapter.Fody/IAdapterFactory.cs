using Mono.Cecil;

namespace AutoAdapter
{
    public interface IAdapterFactory
    {
        TypeDefinition CreateAdapter(AdaptationRequestInstance request);
    }
}