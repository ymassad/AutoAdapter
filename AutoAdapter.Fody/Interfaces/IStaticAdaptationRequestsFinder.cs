using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IStaticAdaptationRequestsFinder
    {
        StaticMethodAdaptationRequestInstance[] FindRequests(MethodDefinition adaptationMethod);
    }
}