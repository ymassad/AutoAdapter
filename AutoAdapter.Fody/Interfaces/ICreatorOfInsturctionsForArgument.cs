using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface ICreatorOfInsturctionsForArgument
    {
        Instruction[] CreateInstructionsForArgument(
            SourceAndTargetParameters parameters,
            ILProcessor ilProcessor,
            Maybe<TypeReference> extraParametersObjectType,
            Maybe<FieldDefinition> extraParametersField);
    }
}