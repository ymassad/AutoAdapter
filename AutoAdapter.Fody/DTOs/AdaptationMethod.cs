using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class AdaptationMethod
    {
        public AdaptationMethod(MethodDefinition method)
        {
            Method = method;
        }

        public MethodDefinition Method { get; }

    }
}