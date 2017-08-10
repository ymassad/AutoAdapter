using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class MethodOnAReferencedType
    {
        public MethodOnAReferencedType(TypeReference referencedType, MethodDefinition methodDefinition)
        {
            ReferencedType = referencedType;
            MethodDefinition = methodDefinition;
        }

        public TypeReference ReferencedType { get; }
        public MethodDefinition MethodDefinition { get; }
    }
}