using System.Linq;
using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public static class ParameterInformationExtractor
    {
        public static ParameterInformation Extract(ParameterDefinition parameter, TypeReference declaringReference)
        {
            return new ParameterInformation(
                parameter.Name,
                parameter.Attributes,
                ExtractType(parameter, declaringReference),
                parameter.IsOptional,
                parameter.HasDefault,
                parameter.HasConstant,
                parameter.Constant,
                parameter.Index);
        }

        private static TypeReference ExtractType(ParameterDefinition parameter, TypeReference declaringReference)
        {
            var parameterParameterType = parameter.ParameterType;

            if (!parameterParameterType.IsGenericParameter)
                return parameterParameterType;

            var genericParamIndex =
                declaringReference
                    .Resolve()
                    .GenericParameters
                    .Select((item, index) => new {item, index})
                    .Where(x => x.item.Name == ((GenericParameter)parameterParameterType).Name)
                    .Select(x => x.index)
                    .First();

            return ((GenericInstanceType) declaringReference).GenericArguments[genericParamIndex];
        }
    }
}