using System.Runtime.InteropServices;
using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public abstract class StaticMethodAdaptationRequest
    {
        protected StaticMethodAdaptationRequest(
            TypeReference sourceStaticClass,
            string sourceStaticMethodName,
            TypeReference destinationType,
            [Optional]Maybe<TypeReference> extraParametersObjectType)
        {
            SourceStaticClass = sourceStaticClass;
            SourceStaticMethodName = sourceStaticMethodName;
            DestinationType = destinationType;
            ExtraParametersObjectType = extraParametersObjectType;
        }

        public TypeReference SourceStaticClass { get; }
        public string SourceStaticMethodName { get; }
        public TypeReference DestinationType { get; }

        public Maybe<TypeReference> ExtraParametersObjectType { get; }

        public static StaticMethodToInterfaceAdaptationRequest InterfaceRequest(
            TypeReference sourceStaticClass,
            string sourceStaticMethodName,
            TypeReference destinationType,
            [Optional] Maybe<TypeReference> extraParametersObjectType)
        {
            return new StaticMethodToInterfaceAdaptationRequest(sourceStaticClass, sourceStaticMethodName, destinationType, extraParametersObjectType);
        }

        public static StaticMethodToDelegateAdaptationRequest DelegateRequest(
            TypeReference sourceStaticClass,
            string sourceStaticMethodName,
            TypeReference destinationType,
            [Optional] Maybe<TypeReference> extraParametersObjectType)
        {
            return new StaticMethodToDelegateAdaptationRequest(sourceStaticClass, sourceStaticMethodName, destinationType, extraParametersObjectType);
        }
    }
}