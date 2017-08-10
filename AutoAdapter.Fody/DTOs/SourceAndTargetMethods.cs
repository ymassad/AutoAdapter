namespace AutoAdapter.Fody.DTOs
{
    public class SourceAndTargetMethods
    {
        public MethodOnAReferencedType TargetMethod { get; }

        public MethodOnAReferencedType SourceMethod { get; }

        public SourceAndTargetMethods(MethodOnAReferencedType sourceMethod, MethodOnAReferencedType targetMethod)
        {
            TargetMethod = targetMethod;
            SourceMethod = sourceMethod;
        }
    }
}