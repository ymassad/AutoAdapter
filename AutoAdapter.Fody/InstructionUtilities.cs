using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody
{
    public static class InstructionUtilities
    {
        public static Instruction[] CreateInstructionsForTypeOfOperator(TypeReference type, ILProcessor ilProcessor, MethodReference getTypeFromHandleMethod)
        {
            return new[]
            {
                ilProcessor.Create(OpCodes.Ldtoken, type),
                ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod)
            };
        }
    }
}
