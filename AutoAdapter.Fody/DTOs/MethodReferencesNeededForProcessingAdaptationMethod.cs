using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class MethodReferencesNeededForProcessingAdaptationMethod
    {
        public MethodReferencesNeededForProcessingAdaptationMethod(MethodReference getTypeFromHandleMethod, MethodReference equalsMethod, MethodReference getTypeMethod, MethodReference exceptionConstructor, MethodReference objectConstructor)
        {
            GetTypeFromHandleMethod = getTypeFromHandleMethod;
            EqualsMethod = equalsMethod;
            GetTypeMethod = getTypeMethod;
            ExceptionConstructor = exceptionConstructor;
            ObjectConstructor = objectConstructor;
        }

        public MethodReference GetTypeFromHandleMethod { get; }
        public MethodReference EqualsMethod { get; }
        public MethodReference GetTypeMethod { get; }
        public MethodReference ExceptionConstructor { get; }
        public MethodReference ObjectConstructor { get; }
    }
}