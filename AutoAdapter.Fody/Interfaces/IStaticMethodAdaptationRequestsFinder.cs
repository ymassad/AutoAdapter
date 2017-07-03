using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IStaticMethodAdaptationRequestsFinder<T> where T : StaticMethodAdaptationRequest
    {
        T[] FindRequests(MethodDefinition adaptationMethod);
    }
}