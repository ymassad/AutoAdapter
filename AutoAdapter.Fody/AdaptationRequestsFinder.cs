using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class AdaptationRequestsFinder : IAdaptationRequestsFinder
    {
        public AdaptationRequestInstance[] FindRequests(MethodDefinition adaptationMethod)
        {
            return adaptationMethod
                .Module
                .GetTypes()
                .SelectMany(x => TypeDefinitionRocks.GetMethods(x))
                .SelectMany(x =>
                    GetInstructionsInMethodThatCallSomeGenericMethod(
                            x,
                            adaptationMethod)
                        .Select(index => new { Method = x, InstructionIndex = index }))
                .Select(x => CreateAdaptationRequestForInstruction(x.Method, x.InstructionIndex))
                .ToArray();
        }

        private int[] GetInstructionsInMethodThatCallSomeGenericMethod(
            MethodDefinition methodToSearch,
            MethodDefinition calledMethod)
        {
            if (!methodToSearch.HasBody)
                return new int[0];

            return
                methodToSearch
                    .Body
                    .Instructions
                    .Select((x, i) => (Instruction: x, Index: i))
                    .Where(x => x.Instruction.OpCode == OpCodes.Call)
                    .Where(x => x.Instruction.Operand is GenericInstanceMethod)
                    .Where(x => ((GenericInstanceMethod)x.Instruction.Operand).ElementMethod == calledMethod)
                    .Select(x => x.Index)
                    .ToArray();
        }

        private AdaptationRequestInstance CreateAdaptationRequestForInstruction(
            MethodDefinition methodToSearch,
            int instructionIndex)
        {
            var bodyInstructions = methodToSearch.Body.Instructions;

            var instruction = bodyInstructions[instructionIndex];

            var genericInstanceMethod = (GenericInstanceMethod)instruction.Operand;

            if (genericInstanceMethod.Parameters.Count == 1)
            {
                return new AdaptationRequestInstance(
                    genericInstanceMethod.GenericArguments[0],
                    genericInstanceMethod.GenericArguments[1]);
            }
            else
            {
                var previousInstruction = bodyInstructions[instructionIndex - 1];

                if (previousInstruction.OpCode != OpCodes.Newobj)
                    throw new Exception("Uexpected to find a Newobj instruction");

                MethodReference constructor = (MethodReference)previousInstruction.Operand;

                return new AdaptationRequestInstance(
                    genericInstanceMethod.GenericArguments[0],
                    genericInstanceMethod.GenericArguments[1],
                    Maybe<TypeReference>.OfValue(constructor.DeclaringType));
            }
        }

    }
}