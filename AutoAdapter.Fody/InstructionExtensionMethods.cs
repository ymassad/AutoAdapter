using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody
{
    public static class InstructionExtensionMethods
    {
        public static MethodReference GetConstructorFromNewObjInstruction(this Instruction instruction)
        {
            if (instruction.OpCode != OpCodes.Newobj)
                throw new Exception("Expected to find a Newobj instruction");

            return (MethodReference) instruction.Operand;
        }

        public static string GetStringLoadedViaLdStr(this Instruction instruction)
        {
            if (instruction.OpCode != OpCodes.Ldstr)
                throw new Exception("Expected to find a LdStr instruction");

            return (string) instruction.Operand;
        }

        public static TypeReference GetTypeTokenLoadedViaLdToken(this Instruction instruction)
        {
            if (instruction.OpCode != OpCodes.Ldtoken)
                throw new Exception("Expected to find a LdToken instruction");

            return (TypeReference) instruction.Operand;
        }
    }
}
