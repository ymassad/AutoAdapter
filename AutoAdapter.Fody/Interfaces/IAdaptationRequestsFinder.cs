using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdaptationRequestsFinder
    {
        AdaptationRequestInstance[] FindRequests(MethodDefinition adaptationMethod);
    }
}