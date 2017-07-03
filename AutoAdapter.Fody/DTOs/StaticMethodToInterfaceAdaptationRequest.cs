using System.Runtime.InteropServices;
using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class StaticMethodToInterfaceAdaptationRequest : StaticMethodAdaptationRequest
    {
        public StaticMethodToInterfaceAdaptationRequest(
            TypeReference sourceStaticClass,
            string sourceStaticMethodName,
            TypeReference destinationType,
            [Optional]Maybe<TypeReference> extraParametersObjectType)
            : base(sourceStaticClass, sourceStaticMethodName, destinationType, extraParametersObjectType)
        {
        }
    }
}