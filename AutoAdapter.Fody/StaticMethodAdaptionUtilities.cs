using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoAdapter.Fody.DTOs;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public static class StaticMethodAdaptionUtilities
    {
        public static StaticMethodAdaptationRequest CreateAdaptationRequestForInstruction(
            MethodDefinition methodToSearch,
            int instructionIndex)
        {
            var bodyInstructions = methodToSearch.Body.Instructions;

            var instruction = bodyInstructions[instructionIndex];

            var genericInstanceMethod = (GenericInstanceMethod)instruction.Operand;

            var destinationType = genericInstanceMethod.GenericArguments[0];

            var sourceStaticClass = bodyInstructions[instructionIndex - 3].GetTypeTokenLoadedViaLdToken();

            var staticMethodName = bodyInstructions[instructionIndex - 1].GetStringLoadedViaLdStr();

            var resolvedDestinationType = destinationType.Resolve();

            if (genericInstanceMethod.Parameters.Count == 2)
            {
                if(instructionIndex < 3)
                    throw new Exception("Unexpected instruction index");

                if (resolvedDestinationType.IsInterface)
                {
                    return StaticMethodAdaptationRequest.InterfaceRequest(
                        sourceStaticClass: sourceStaticClass,
                        sourceStaticMethodName: staticMethodName,
                        destinationType: destinationType);
                }

                if (TypeUtilities.IsDelegateType(resolvedDestinationType.Resolve()))
                {
                    return StaticMethodAdaptationRequest.DelegateRequest(
                        sourceStaticClass: sourceStaticClass,
                        sourceStaticMethodName: staticMethodName,
                        destinationType: destinationType);
                }

                throw new Exception("Unsupported destination type");
            }
            else
            {
                if (instructionIndex < 4)
                    throw new Exception("Unexpected instruction index");

                var extraParametersObjectType =
                    bodyInstructions[instructionIndex - 4].GetConstructorFromNewObjInstruction().DeclaringType;

                if (resolvedDestinationType.IsInterface)
                {
                    return StaticMethodAdaptationRequest.InterfaceRequest(
                        sourceStaticClass: sourceStaticClass,
                        sourceStaticMethodName: staticMethodName,
                        destinationType: destinationType,
                        extraParametersObjectType: extraParametersObjectType);
                }

                if (TypeUtilities.IsDelegateType(resolvedDestinationType.Resolve()))
                {
                    return StaticMethodAdaptationRequest.DelegateRequest(
                        sourceStaticClass: sourceStaticClass,
                        sourceStaticMethodName: staticMethodName,
                        destinationType: destinationType,
                        extraParametersObjectType: extraParametersObjectType);
                }

                throw new Exception("Unsupported destination type");
            }
        }
    }
}
