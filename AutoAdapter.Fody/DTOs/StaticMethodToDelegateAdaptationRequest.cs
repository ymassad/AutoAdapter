using System.Runtime.InteropServices;
using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class StaticMethodToDelegateAdaptationRequest : StaticMethodAdaptationRequest
    {
        public StaticMethodToDelegateAdaptationRequest(
            TypeReference sourceStaticClass,
            string sourceStaticMethodName,
            TypeReference destinationType,
            [Optional]Maybe<TypeReference> extraParametersObjectType)
            : base(sourceStaticClass, sourceStaticMethodName, destinationType, extraParametersObjectType)
        {
        }
    }
}