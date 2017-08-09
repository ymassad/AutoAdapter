using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class SourceAndTargetMethods
    {
        public MethodDefinition TargetMethod { get; }
        public MethodDefinition SourceMethod { get; }
        public TypeReference SourceType { get; }
        public TypeReference TargetType { get; }

        public SourceAndTargetMethods(MethodDefinition targetMethod, MethodDefinition sourceMethod, TypeReference sourceType, TypeReference targetType)
        {
            TargetMethod = targetMethod;
            SourceMethod = sourceMethod;
            SourceType = sourceType;
            TargetType = targetType;
        }
    }
}