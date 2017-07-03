using System.Collections.Generic;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodAdaptationMethodsFinder : IAdaptationMethodsFinder<StaticMethodAdaptationMethod>
    {
        public StaticMethodAdaptationMethod[] FindAdaptationMethods(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition
                .GetAllMethods()
                .Where(x => x.GenericParameters.Count == 1)
                .Where(x => x.ReturnType == x.GenericParameters[0])
                .Where(x => x.HasATypeAndaStringParameters() || x.HasAnObjectAndATypeAndAStringParameters())
                .Where(ExtensionMethods.HasCustomAttribute)
                .Select(x => new StaticMethodAdaptationMethod(x))
                .ToArray();
        }
    }
}