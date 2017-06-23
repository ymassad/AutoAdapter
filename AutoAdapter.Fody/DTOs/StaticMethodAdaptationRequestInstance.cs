using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class StaticMethodAdaptationRequestInstance
    {
        public StaticMethodAdaptationRequestInstance(
            TypeReference sourceStaticClass,
            string sourceStaticMethodName,
            TypeReference destinationType,
            Maybe<TypeReference> extraParametersObjectType)
        {
            SourceStaticClass = sourceStaticClass;
            SourceStaticMethodName = sourceStaticMethodName;
            DestinationType = destinationType;
            ExtraParametersObjectType = extraParametersObjectType;
        }

        public StaticMethodAdaptationRequestInstance(
            TypeReference sourceStaticClass,
            string sourceStaticMethodName,
            TypeReference destinationType)
            :this(sourceStaticClass, sourceStaticMethodName, destinationType, Maybe<TypeReference>.NoValue())
        {
        }

        public TypeReference SourceStaticClass { get; }
        public string SourceStaticMethodName { get; }
        public TypeReference DestinationType { get; }

        public Maybe<TypeReference> ExtraParametersObjectType { get; }
    }
}