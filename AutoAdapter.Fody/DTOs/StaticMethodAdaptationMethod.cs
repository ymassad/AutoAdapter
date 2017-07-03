using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class StaticMethodAdaptationMethod : AdaptationMethod
    {
        public StaticMethodAdaptationMethod(MethodDefinition method) : base(method)
        {
        }
    }
}