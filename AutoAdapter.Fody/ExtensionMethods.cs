using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public static class ExtensionMethods
    {
        public static bool HasCustomAttribute(this MethodDefinition x)
        {
            return x.CustomAttributes.Any(a => a.AttributeType.Name == "AdapterMethodAttribute");
        }

        public static IEnumerable<MethodDefinition> GetAllMethods(this ModuleDefinition moduleDefinition)
        {
            return moduleDefinition
                .GetTypes()
                .SelectMany(x => x.GetMethods());
        }

        public static bool HasATypeAndaStringParameters(this MethodDefinition x)
        {
            return x.Parameters.Count == 2 && x.Parameters[0].ParameterType.FullName == "System.Type" && x.Parameters[1].ParameterType.FullName == "System.String";
        }

        public static bool HasAnObjectAndATypeAndAStringParameters(this MethodDefinition x)
        {
            return x.Parameters.Count == 3 && x.Parameters[0].ParameterType.FullName == "System.Object" && x.Parameters[1].ParameterType.FullName == "System.Type" && x.Parameters[2].ParameterType.FullName == "System.String";
        }
    }
}
