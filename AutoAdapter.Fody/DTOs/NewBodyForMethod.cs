using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody.DTOs
{
    public class NewBodyForMethod
    {
        public NewBodyForMethod(MethodDefinition method, Instruction[] newBody)
        {
            Method = method;
            NewBody = newBody;
        }

        public MethodDefinition Method { get; }
        public Instruction[] NewBody { get; }
    }
}