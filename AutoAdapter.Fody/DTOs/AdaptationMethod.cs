using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public abstract class AdaptationMethod
    {
        protected AdaptationMethod(MethodDefinition method)
        {
            Method = method;
        }

        public MethodDefinition Method { get; }
    }
}