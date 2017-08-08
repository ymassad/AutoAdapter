using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody.DTOs
{
    public class TypesToAddToModuleAndNewBodyForAdaptationMethod
    {
        public TypesToAddToModuleAndNewBodyForAdaptationMethod(TypeDefinition[] typesToAdd, Instruction[] newBodyForAdaptationMethod)
        {
            TypesToAdd = typesToAdd;
            NewBodyForAdaptationMethod = newBodyForAdaptationMethod;
        }

        public TypeDefinition[] TypesToAdd { get; }
        public Instruction[] NewBodyForAdaptationMethod { get; }
    }
}