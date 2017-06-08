using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdapterFactory
    {
        TypeDefinition CreateAdapter(AdaptationRequestInstance request);
    }
}