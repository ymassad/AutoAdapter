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
        public RefTypeToInterfaceAdaptationRequest[] FindRequests(MethodDefinition adaptationMethod)
        {
            return adaptationMethod
                .Module
                .GetTypes()
                .SelectMany(x => x.GetMethods())
                .SelectMany(x =>
                    MethodInvocationFinder.GetInstructionsInMethodThatCallSomeGenericMethod(
                        x,
                        adaptationMethod)
                    .Select(index => new { Method = x, InstructionIndex = index }))
                .Select(x => CreateAdaptationRequestForInstruction(x.Method, x.InstructionIndex))
                .ToArray();
        }

        private RefTypeToInterfaceAdaptationRequest CreateAdaptationRequestForInstruction(
            MethodDefinition methodToSearch,
            int instructionIndex)
        {
            var bodyInstructions = methodToSearch.Body.Instructions;

            var instruction = bodyInstructions[instructionIndex];

            var genericInstanceMethod = (GenericInstanceMethod)instruction.Operand;

            if (genericInstanceMethod.Parameters.Count == 1)
            {
                return new RefTypeToInterfaceAdaptationRequest(
                    genericInstanceMethod.GenericArguments[0],
                    genericInstanceMethod.GenericArguments[1]);
            }
            else
            {
                var previousInstruction = bodyInstructions[instructionIndex - 1];

                if (previousInstruction.OpCode != OpCodes.Newobj)
                    throw new Exception("Uexpected to find a Newobj instruction");

                MethodReference constructor = (MethodReference)previousInstruction.Operand;

                return new RefTypeToInterfaceAdaptationRequest(
                    genericInstanceMethod.GenericArguments[0],
                    genericInstanceMethod.GenericArguments[1],
                    Maybe<TypeReference>.OfValue(constructor.DeclaringType));
            }
        }

    }
}