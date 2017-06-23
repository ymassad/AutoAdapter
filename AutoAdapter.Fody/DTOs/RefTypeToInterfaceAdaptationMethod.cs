using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class RefTypeToInterfaceAdaptationMethod : AdaptationMethod
    {
        public RefTypeToInterfaceAdaptationMethod(MethodDefinition method) : base(method)
        {
        }
    }
}