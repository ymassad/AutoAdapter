using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IAdapterMethodsCreator
    {
        MethodDefinition[] CreateAdapterMethods(
            RefTypeToInterfaceAdaptationRequest request,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField);
    }
}