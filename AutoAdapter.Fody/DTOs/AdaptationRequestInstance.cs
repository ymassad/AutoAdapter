using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class AdaptationRequestInstance
    {
        public AdaptationRequestInstance(
            TypeReference sourceType, TypeReference destinationType, Maybe<TypeReference> extraParametersObjectType)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            ExtraParametersObjectType = extraParametersObjectType;
        }

        public AdaptationRequestInstance(TypeReference sourceType, TypeReference destinationType)
            : this(sourceType, destinationType, Maybe<TypeReference>.NoValue())
        {
        }

        public TypeReference SourceType { get; }
        public TypeReference DestinationType { get; }

        public Maybe<TypeReference> ExtraParametersObjectType { get; }
    }
}