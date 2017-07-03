using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public static class TypeUtilities
    {
        public static bool IsDelegateType(TypeDefinition type)
        {
            while (type != null)
            {
                if (type.FullName == "System.Delegate")
                    return true;

                type = type.BaseType?.Resolve();
            }

            return false;
        }
    }
}
