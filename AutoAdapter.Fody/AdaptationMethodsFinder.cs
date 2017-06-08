using System.Linq;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class AdaptationMethodsFinder : IAdaptationMethodsFinder
    {
        public MethodDefinition[] FindAdaptationMethods(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition
                .GetTypes()
                .SelectMany(x => TypeDefinitionRocks.GetMethods(x))
                .Where(x => x.Parameters.Count > 0)
                .Where(x => x.GenericParameters.Count == 2)
                .Where(x => x.ReturnType == x.GenericParameters[1])
                .Where(x => x.Parameters[0].ParameterType == x.GenericParameters[0])
                .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == "AdapterMethodAttribute"))
                .ToArray();
        }
    }
}