using System.Linq;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public class SourceAndTargetMethodsMapper : ISourceAndTargetMethodsMapper
    {
        public SourceAndTargetMethods[] CreateMap(
            TypeDefinition resolvedDestinationType,
            TypeDefinition resolvedSourceType)
        {
            var sourceMethods =
                resolvedSourceType.Methods
                    .Where(x => x.IsPublic)
                    .Where(x => !x.IsConstructor)
                    .Where(x => !x.IsStatic)
                    .ToArray();

            if (sourceMethods.Length == 1 && resolvedDestinationType.Methods.Count == 1)
            {
                return new[] {new SourceAndTargetMethods(resolvedDestinationType.Methods[0], sourceMethods[0])};
            }

            return resolvedDestinationType.Methods
                .Select(targetMethod =>
                    new SourceAndTargetMethods(
                        targetMethod,
                        sourceMethods.Single(sourceMethod => sourceMethod.Name == targetMethod.Name)))
                .ToArray();
        }
    }
}