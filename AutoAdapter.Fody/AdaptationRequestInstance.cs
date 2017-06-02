using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public class AdaptationRequestInstance
    {
        public AdaptationRequestInstance(
            TypeReference sourceType, TypeReference destinationType, Maybe<TypeReference> extraParametersType)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            ExtraParametersType = extraParametersType;
        }

        public AdaptationRequestInstance(TypeReference sourceType, TypeReference destinationType)
            : this(sourceType, destinationType, Maybe<TypeReference>.NoValue())
        {
        }

        public TypeReference SourceType { get; }
        public TypeReference DestinationType { get; }

        public Maybe<TypeReference> ExtraParametersType { get; }
    }
}