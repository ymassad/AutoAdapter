using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class ChangesToModule
    {
        public ChangesToModule(TypeDefinition[] typesToAdd, NewBodyForMethod[] newMethodBodies)
        {
            TypesToAdd = typesToAdd;
            NewMethodBodies = newMethodBodies;
        }

        public TypeDefinition[] TypesToAdd { get; }
        public NewBodyForMethod[] NewMethodBodies { get; }
    }
}