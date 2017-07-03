using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class MembersToAdd
    {
        public MembersToAdd(MethodDefinition[] methodsToAdd, FieldDefinition[] fieldsToAdd)
        {
            MethodsToAdd = methodsToAdd;
            FieldsToAdd = fieldsToAdd;
        }

        public MethodDefinition[] MethodsToAdd { get; }

        public FieldDefinition[] FieldsToAdd { get; }
    }
}