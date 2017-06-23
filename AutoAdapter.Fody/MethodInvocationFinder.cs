using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody
{
    public static class MethodInvocationFinder
    {
        public static int[] GetInstructionsInMethodThatCallSomeGenericMethod(
            MethodDefinition methodToSearch,
            MethodDefinition calledMethod)
        {
            if (!methodToSearch.HasBody)
                return new int[0];

            return
                methodToSearch
                    .Body
                    .Instructions
                    .Select((x, i) => (Instruction: x, Index: i))
                    .Where(x => x.Instruction.OpCode == OpCodes.Call)
                    .Where(x => x.Instruction.Operand is GenericInstanceMethod)
                    .Where(x => ((GenericInstanceMethod)x.Instruction.Operand).ElementMethod == calledMethod)
                    .Select(x => x.Index)
                    .ToArray();
        }
    }
}
