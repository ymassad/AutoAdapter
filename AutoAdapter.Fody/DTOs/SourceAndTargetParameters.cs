using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class SourceAndTargetParameters
    {
        public ParameterDefinition SourceParameter { get; }
        public Maybe<ParameterDefinition> TargetParameter { get; }

        public SourceAndTargetParameters(ParameterDefinition sourceParameter, Maybe<ParameterDefinition> targetParameter)
        {
            SourceParameter = sourceParameter;
            TargetParameter = targetParameter;
        }
    }
}