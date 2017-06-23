using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class StaticMethodToInterfaceAdaptationMethod : AdaptationMethod
    {
        public StaticMethodToInterfaceAdaptationMethod(MethodDefinition method) : base(method)
        {
        }
    }
}