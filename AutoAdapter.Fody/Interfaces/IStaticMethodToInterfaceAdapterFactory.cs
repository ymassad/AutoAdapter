using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IStaticMethodToInterfaceAdapterFactory<T> where T : StaticMethodAdaptationRequest
    {
        TypeDefinition CreateAdapter(ModuleDefinition module, T request);
    }
}