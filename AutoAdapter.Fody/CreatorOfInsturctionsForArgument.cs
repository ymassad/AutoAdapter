using System;
using System.Collections.Generic;
using System.Linq;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody
{
    public class CreatorOfInsturctionsForArgument : ICreatorOfInsturctionsForArgument
    {
        public Instruction[] CreateInstructionsForArgument(
            SourceAndTargetParameters parameters,
            ILProcessor ilProcessor,
            Maybe<TypeReference> extraParametersObjectType,
            Maybe<FieldDefinition> extraParametersField)
        {
            if (parameters.TargetParameter.HasValue)
                return CreateInstructionsForArgumentUsingTargetParameter(parameters, ilProcessor);

            if (extraParametersObjectType.HasValue)
            {
                return CreateInsturctionsForArgumentUsingExtraParametersObject(parameters, ilProcessor, extraParametersObjectType.GetValue(), extraParametersField);
            }

            if (parameters.SourceParameter.IsOptional &&
                parameters.SourceParameter.HasDefault &&
                parameters.SourceParameter.HasConstant)
            {
                return CreateInsturctionsArgumentUsingDefaultConstantValueOfSourceParameter(parameters, ilProcessor);
            }

            throw new Exception(
                $"Source parameter {parameters.SourceParameter.Name} is not optional with a default constant value and no extra parameters object is supplied");
        }

        private Instruction[] CreateInstructionsForArgumentUsingTargetParameter(
            SourceAndTargetParameters parameters,
            ILProcessor ilProcessor)
        {
            return new[]
            {
                ilProcessor.Create(OpCodes.Ldarg, parameters.TargetParameter.GetValue().Index + 1)
            };
        }

        private Instruction[] CreateInsturctionsArgumentUsingDefaultConstantValueOfSourceParameter(
            SourceAndTargetParameters parameters,
            ILProcessor ilProcessor)
        {
            switch (parameters.SourceParameter.ParameterType.FullName)
            {
                case "System.Int32":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_I4,
                            (int) parameters.SourceParameter.Constant)
                    };
                case "System.Int64":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_I8,
                            (long) parameters.SourceParameter.Constant)
                    };
                case "System.String":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldstr,
                            (string) parameters.SourceParameter.Constant)
                    };
                case "System.Single":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_R4,
                            (float) parameters.SourceParameter.Constant)
                    };
                case "System.Double":
                    return new[]
                    {
                        ilProcessor.Create(
                            OpCodes.Ldc_R8,
                            (double) parameters.SourceParameter.Constant)
                    };
            }
            throw new Exception("Unsupported optional parameter constant type");
        }

        public Instruction[] CreateInsturctionsForArgumentUsingExtraParametersObject(
            SourceAndTargetParameters parameters,
            ILProcessor ilProcessor,
            TypeReference extraParametersObjectType,
            Maybe<FieldDefinition> extraParametersField)
        {
            var resolvedExtraParametersObjectType = extraParametersObjectType.Resolve();

            var instructions = new List<Instruction>();

            instructions.Add(ilProcessor.Create(OpCodes.Ldarg_0));

            instructions.Add(ilProcessor.Create(OpCodes.Ldfld, extraParametersField.GetValue()));

            var propertyOnExtraParametersObject =
                resolvedExtraParametersObjectType.Properties
                    .FirstOrDefault(p => p.Name == parameters.SourceParameter.Name);

            if (propertyOnExtraParametersObject == null)
                throw new Exception(
                    $"Could not find property {parameters.SourceParameter.Name} on the extra parameters object");

            var propertyGetMethod = propertyOnExtraParametersObject.GetMethod;

            var propertyReturnType = propertyGetMethod.MethodReturnType.ReturnType;

            var propertyGetMethodReference =
                new MethodReference(
                        propertyGetMethod.Name,
                        propertyReturnType,
                        extraParametersObjectType)
                    { HasThis = true };

            instructions.Add(ilProcessor.Create(OpCodes.Callvirt, propertyGetMethodReference));

            return instructions.ToArray();
        }
    }
}