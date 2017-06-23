using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodAdaptationRequestsFinder : IStaticAdaptationRequestsFinder
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

                var loadMethodNameStringInstruction =  bodyInstructions[instructionIndex - 1]; 

                if(loadMethodNameStringInstruction.OpCode != OpCodes.Ldstr)
                    throw new Exception("Expected to find a LdStr instruction");

                var methodName = (string)loadMethodNameStringInstruction.Operand;

                var loadStaticClassTypeTokenInstruction = bodyInstructions[instructionIndex - 3];

                if(loadStaticClassTypeTokenInstruction.OpCode != OpCodes.Ldtoken)
                    throw new Exception("Expected to find a LdToken instruction");

                var staticClassTypeReference = (TypeReference)loadStaticClassTypeTokenInstruction.Operand;

                return new StaticMethodAdaptationRequestInstance(
                    staticClassTypeReference,
                    methodName,
                    genericInstanceMethod.GenericArguments[0]);
            }
            else
            {
                if (instructionIndex < 4)
                    throw new Exception("Unexpected instruction index");

                var newExtraParametersObjectInstruction = bodyInstructions[instructionIndex - 1];

                if (newExtraParametersObjectInstruction.OpCode != OpCodes.Newobj)
                    throw new Exception("Expected to find a Newobj instruction");

                MethodReference extraParametersObjectConstructor = (MethodReference)newExtraParametersObjectInstruction.Operand;

                var loadMethodNameStringInstruction = bodyInstructions[instructionIndex - 2];

                if (loadMethodNameStringInstruction.OpCode != OpCodes.Ldstr)
                    throw new Exception("Expected to find a LdStr instruction");

                var methodName = (string)loadMethodNameStringInstruction.Operand;

                var loadStaticClassTypeTokenInstruction = bodyInstructions[instructionIndex - 4];

                if (loadStaticClassTypeTokenInstruction.OpCode != OpCodes.Ldtoken)
                    throw new Exception("Expected to find a LdToken instruction");

                var staticClassTypeReference = (TypeReference)loadStaticClassTypeTokenInstruction.Operand;

                return new StaticMethodAdaptationRequestInstance(
                    staticClassTypeReference,
                    methodName,
                    genericInstanceMethod.GenericArguments[0],
                    Maybe<TypeReference>.OfValue(extraParametersObjectConstructor.DeclaringType));
            }
        }

    }
}