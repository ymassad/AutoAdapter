using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IStaticMethodAdapterFactory
    {
        TypeDefinition CreateAdapter(ModuleDefinition module, StaticMethodAdaptationRequestInstance request);
    }
}