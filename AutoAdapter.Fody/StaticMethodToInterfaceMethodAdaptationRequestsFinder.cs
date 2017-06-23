using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodToInterfaceMethodAdaptationRequestsFinder : IStaticAdaptationRequestsFinder
    {
        public StaticMethodAdaptationRequestInstance[] FindRequests(MethodDefinition adaptationMethod)
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

        private StaticMethodAdaptationRequestInstance CreateAdaptationRequestForInstruction(
            MethodDefinition methodToSearch,
            int instructionIndex)
        {
            var bodyInstructions = methodToSearch.Body.Instructions;

            var instruction = bodyInstructions[instructionIndex];

            var genericInstanceMethod = (GenericInstanceMethod)instruction.Operand;

            if (genericInstanceMethod.Parameters.Count == 2)
            {
                if(instructionIndex < 3)
                    throw new Exception("Unexpected instruction index");

                return new StaticMethodAdaptationRequestInstance(
                    sourceStaticClass: bodyInstructions[instructionIndex - 3].GetTypeTokenLoadedViaLdToken(),
                    sourceStaticMethodName: bodyInstructions[instructionIndex - 1].GetStringLoadedViaLdStr(),
                    destinationType: genericInstanceMethod.GenericArguments[0]);
            }
            else
            {
                if (instructionIndex < 4)
                    throw new Exception("Unexpected instruction index");

                return new StaticMethodAdaptationRequestInstance(
                    sourceStaticClass: bodyInstructions[instructionIndex - 4].GetTypeTokenLoadedViaLdToken(),
                    sourceStaticMethodName: bodyInstructions[instructionIndex - 2].GetStringLoadedViaLdStr(),
                    destinationType: genericInstanceMethod.GenericArguments[0],
                    extraParametersObjectType: Maybe<TypeReference>.OfValue(
                        bodyInstructions[instructionIndex - 1].GetConstructorFromNewObjInstruction().DeclaringType));
            }
        }
    }
}