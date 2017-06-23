using System.Linq;
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

        public static ChangesToModule Empty() => new ChangesToModule(new TypeDefinition[0], new NewBodyForMethod[0]);

        public static ChangesToModule Merge(ChangesToModule changes1, ChangesToModule changes2)
            => new ChangesToModule(
                changes1.TypesToAdd.Concat(changes2.TypesToAdd).ToArray(),
                changes1.NewMethodBodies.Concat(changes2.NewMethodBodies).ToArray());
    }
}