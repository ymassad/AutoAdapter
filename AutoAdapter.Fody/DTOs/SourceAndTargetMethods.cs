using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class SourceAndTargetMethods
    {
        public MethodDefinition TargetMethod { get; }
        public MethodDefinition SourceMethod { get; }

        public SourceAndTargetMethods(MethodDefinition targetMethod, MethodDefinition sourceMethod)
        {
            TargetMethod = targetMethod;
            SourceMethod = sourceMethod;
        }
    }
}