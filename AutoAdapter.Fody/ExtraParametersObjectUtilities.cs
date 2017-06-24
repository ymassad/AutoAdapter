using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public static class ExtraParametersObjectUtilities
    {
        public static FieldDefinition CreateExtraParametersField(TypeReference extraParametersObjectType)
        {
            return 
                new FieldDefinition(
                    "extraParameters",
                    FieldAttributes.InitOnly | FieldAttributes.Private,
                    extraParametersObjectType);
        }
    }
}
