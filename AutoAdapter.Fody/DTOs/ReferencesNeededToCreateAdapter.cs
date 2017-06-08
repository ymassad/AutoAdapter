using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class ReferencesNeededToCreateAdapter
    {
        public ReferencesNeededToCreateAdapter(TypeReference objectClassReference, TypeReference voidReference, MethodReference objectConstructorReference)
        {
            ObjectClassReference = objectClassReference;
            VoidReference = voidReference;
            ObjectConstructorReference = objectConstructorReference;
        }

        public TypeReference ObjectClassReference { get; }
        public TypeReference VoidReference { get; }
        public MethodReference ObjectConstructorReference { get; }
    }
}