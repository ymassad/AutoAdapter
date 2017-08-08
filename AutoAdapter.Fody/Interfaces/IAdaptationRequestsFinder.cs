using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdaptationRequestsFinder
    {
        RefTypeToInterfaceAdaptationRequest[] FindRequests(MethodDefinition adaptationMethod);
    }
}