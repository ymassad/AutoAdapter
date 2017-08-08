using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IRefTypeToInterfaceAdaptationRequestsFinder
    {
        RefTypeToInterfaceAdaptationRequest[] FindRequests(MethodDefinition adaptationMethod);
    }
}