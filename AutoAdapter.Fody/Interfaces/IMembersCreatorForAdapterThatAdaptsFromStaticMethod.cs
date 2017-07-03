using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IMembersCreatorForAdapterThatAdaptsFromStaticMethod
    {
        MembersToAdd CreateMembers(
            ModuleDefinition module,
            SourceAndTargetMethods sourceAndTargetMethods,
            Maybe<TypeReference> extraParametersObjectType);
    }
}