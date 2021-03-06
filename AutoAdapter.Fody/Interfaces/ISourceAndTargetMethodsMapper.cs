using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface ISourceAndTargetMethodsMapper
    {
        SourceAndTargetMethods[] CreateMap(TypeReference destinationType, TypeReference sourceType);
    }
}