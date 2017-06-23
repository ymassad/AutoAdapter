using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class RefTypeToInterfaceAdaptationMethodsFinder : IAdaptationMethodsFinder<RefTypeToInterfaceAdaptationMethod>
    {
        public RefTypeToInterfaceAdaptationMethod[] FindAdaptationMethods(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition
                .GetTypes()
                .SelectMany(x => x.GetMethods())
                .Where(x => x.Parameters.Count > 0)
                .Where(x => x.GenericParameters.Count == 2)
                .Where(x => x.ReturnType == x.GenericParameters[1])
                .Where(x => x.Parameters[0].ParameterType == x.GenericParameters[0])
                .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == "AdapterMethodAttribute"))
                .Select(x => new RefTypeToInterfaceAdaptationMethod(x))
                .ToArray();
        }
    }
}