using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IRefTypeToInterfaceAdapterFactory
    {
        TypeDefinition CreateAdapter(ModuleDefinition module, RefTypeToInterfaceAdaptationRequest request);
    }
}