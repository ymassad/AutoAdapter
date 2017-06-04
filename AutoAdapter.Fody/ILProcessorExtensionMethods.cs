using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody
{
    public static class ILProcessorExtensionMethods
    {
        public static void AppendRange(this ILProcessor ilprocessor, IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                ilprocessor.Append(instruction);
            }
        }
    }
}
