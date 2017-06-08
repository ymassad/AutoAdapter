using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody.DTOs
{
    public class TypesToAddToModuleAndNewBodyForAdaptation
    {
        public TypesToAddToModuleAndNewBodyForAdaptation(TypeDefinition[] typesToAdd, Instruction[] newBodyForAdaptationMethod)
        {
            TypesToAdd = typesToAdd;
            NewBodyForAdaptationMethod = newBodyForAdaptationMethod;
        }

        public TypeDefinition[] TypesToAdd { get; }
        public Instruction[] NewBodyForAdaptationMethod { get; }
    }
}