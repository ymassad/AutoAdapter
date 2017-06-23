using System.Linq;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodAdaptationMethodsFinder : IStaticAdaptationMethodsFinder
    {
        public MethodDefinition[] FindStaticAdaptationMethods(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition
                .GetTypes()
                .SelectMany(x => x.GetMethods())
                .Where(x => x.Parameters.Count > 1)
                .Where(x => x.GenericParameters.Count == 1)
                .Where(x => x.ReturnType == x.GenericParameters[0])
                .Where(x => x.Parameters[0].ParameterType.FullName == "System.Type")
                .Where(x => x.Parameters[1].ParameterType.FullName == "System.String")
                .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == "AdapterMethodAttribute"))
                .ToArray();
        }
    }
}