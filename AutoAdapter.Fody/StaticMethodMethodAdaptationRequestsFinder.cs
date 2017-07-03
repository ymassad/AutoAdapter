using System;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodMethodAdaptationRequestsFinder<T> : IStaticMethodAdaptationRequestsFinder<T> where T: StaticMethodAdaptationRequest
    {
        public T[] FindRequests(MethodDefinition adaptationMethod)
        {
            return adaptationMethod
                .Module
                .GetAllMethods()
                .SelectMany(x =>
                    MethodInvocationFinder.GetInstructionsInMethodThatCallSomeGenericMethod(
                            x,
                            adaptationMethod)
                        .Select(index => new { Method = x, InstructionIndex = index }))
                .Select(x => StaticMethodAdaptionUtilities.CreateAdaptationRequestForInstruction(x.Method, x.InstructionIndex))
                .OfType<T>()
                .ToArray();
        }
    }
}