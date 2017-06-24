using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodToInterfaceAdaptationMethodsFinder : IAdaptationMethodsFinder<StaticMethodToInterfaceAdaptationMethod>
    {
        public StaticMethodToInterfaceAdaptationMethod[] FindAdaptationMethods(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition
                .GetTypes()
                .SelectMany(x => x.GetMethods())
                .Where(x => x.GenericParameters.Count == 1)
                .Where(x => x.ReturnType == x.GenericParameters[0])
                .Where(x => HasATypeAndaStringParameters(x) || HasAnObjectAndATypeAndAStringParameters(x))
                .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == "AdapterMethodAttribute"))
                .Select(x => new StaticMethodToInterfaceAdaptationMethod(x))
                .ToArray();
        }

        private static bool HasATypeAndaStringParameters(MethodDefinition x)
        {
            return x.Parameters.Count == 2 && x.Parameters[0].ParameterType.FullName == "System.Type" && x.Parameters[1].ParameterType.FullName == "System.String";
        }

        private static bool HasAnObjectAndATypeAndAStringParameters(MethodDefinition x)
        {
            return x.Parameters.Count == 3 && x.Parameters[0].ParameterType.FullName == "System.Object" && x.Parameters[1].ParameterType.FullName == "System.Type" && x.Parameters[2].ParameterType.FullName == "System.String";
        }
    }
}