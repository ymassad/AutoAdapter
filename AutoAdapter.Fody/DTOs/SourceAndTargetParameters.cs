using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class SourceAndTargetParameters
    {
        public ParameterInformation SourceParameter { get; }
        public Maybe<ParameterInformation> TargetParameter { get; }

        public SourceAndTargetParameters(ParameterInformation sourceParameter, Maybe<ParameterInformation> targetParameter)
        {
            SourceParameter = sourceParameter;
            TargetParameter = targetParameter;
        }
    }
}