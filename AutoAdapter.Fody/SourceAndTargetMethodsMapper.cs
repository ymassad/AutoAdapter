using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public class SourceAndTargetMethodsMapper : ISourceAndTargetMethodsMapper
    {
        public SourceAndTargetMethods[] CreateMap(TypeReference destinationType, TypeReference sourceType)
        {
            var resolvedDestinationType = destinationType.Resolve();

            var resolvedSourceType = sourceType.Resolve();

            var sourceMethods =
                resolvedSourceType.Methods
                    .Where(x => x.IsPublic)
                    .Where(x => !x.IsConstructor)
                    .Where(x => !x.IsStatic)
                    .ToArray();

            if (sourceMethods.Length == 1 && resolvedDestinationType.Methods.Count == 1)
            {
                return new[] {new SourceAndTargetMethods(new MethodOnAReferencedType(sourceType, sourceMethods[0]), new MethodOnAReferencedType(destinationType, resolvedDestinationType.Methods[0]))};
            }

            return resolvedDestinationType.Methods
                .Select(targetMethod =>
                    new SourceAndTargetMethods(new MethodOnAReferencedType(sourceType, sourceMethods.Single(sourceMethod => sourceMethod.Name == targetMethod.Name)), new MethodOnAReferencedType(destinationType, targetMethod)))
                .ToArray();
        }
    }
}