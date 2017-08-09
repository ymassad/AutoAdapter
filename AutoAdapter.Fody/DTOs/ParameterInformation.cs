using Mono.Cecil;

namespace AutoAdapter.Fody.DTOs
{
    public class ParameterInformation
    {
        public ParameterInformation(string name, ParameterAttributes attributes, TypeReference parameterType, bool isOptional, bool hasDefault, bool hasConstant, object constant, int index)
        {
            Name = name;
            Attributes = attributes;
            ParameterType = parameterType;
            IsOptional = isOptional;
            HasDefault = hasDefault;
            HasConstant = hasConstant;
            Constant = constant;
            Index = index;
        }

        public string Name { get; }
        public ParameterAttributes Attributes { get; } 
        public TypeReference ParameterType { get; }
        public bool IsOptional { get; }
        public bool HasDefault { get; }
        public bool HasConstant { get; }
        public object Constant { get; }
        public int Index { get; }
    }
}