using AutoAdapter.Fody.DTOs;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface ICreatorOfInsturctionsForArgument
    {
        Instruction[] CreateInstructionsForArgument(
            SourceAndTargetParameters parameters,
            ILProcessor ilProcessor,
            Maybe<FieldDefinition> extraParametersField);
    }
}