using System.Collections.Generic;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class AdaptationMethodProcessor : IAdaptationMethodProcessor
    {
        private readonly IAdapterFactory adapterFactory;
        private readonly IAdaptationRequestsFinder adaptationRequestsFinder;

        public AdaptationMethodProcessor(IAdapterFactory adapterFactory, IAdaptationRequestsFinder adaptationRequestsFinder)
        {
            this.adapterFactory = adapterFactory;
            this.adaptationRequestsFinder = adaptationRequestsFinder;
        }

        public TypesToAddToModuleAndNewBodyForAdaptation ProcessAdaptationMethod(
            MethodDefinition adaptationMethod,
            MethodReferencesNeededForProcessingAdaptationMethod methodReferences)
        {
            var typesToAdd = new List<TypeDefinition>();

            var newBodyInstructions = new List<Instruction>();

            var adaptationRequests = adaptationRequestsFinder.FindRequests(adaptationMethod);

            var ilProcessor = adaptationMethod.Body.GetILProcessor();

            foreach (var request in adaptationRequests)
            {
                var adapterType = adapterFactory.CreateAdapter(request);

                typesToAdd.Add(adapterType);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, adaptationMethod.GenericParameters[0]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, methodReferences.GetTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.SourceType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, methodReferences.GetTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, methodReferences.EqualsMethod));

                var exitLabel = ilProcessor.Create(OpCodes.Nop);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, adaptationMethod.GenericParameters[1]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, methodReferences.GetTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.DestinationType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, methodReferences.GetTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, methodReferences.EqualsMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                if (request.ExtraParametersObjectType.HasValue)
                {
                    newBodyInstructions.Add(ilProcessor.Create(adaptationMethod.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, methodReferences.GetTypeMethod));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.ExtraParametersObjectType.GetValue()));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, methodReferences.GetTypeFromHandleMethod));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, methodReferences.EqualsMethod));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));
                }

                newBodyInstructions.Add(ilProcessor.Create(adaptationMethod.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Box, adaptationMethod.GenericParameters[0]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Castclass, request.SourceType));

                if (request.ExtraParametersObjectType.HasValue)
                {
                    newBodyInstructions.Add(ilProcessor.Create(adaptationMethod.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Castclass, request.ExtraParametersObjectType.GetValue()));
                }

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Unbox_Any, adaptationMethod.GenericParameters[1]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ret));

                newBodyInstructions.Add(exitLabel);
            }

            newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldstr, "Adaptation request is not registered"));

            newBodyInstructions.Add(ilProcessor.Create(
                OpCodes.Newobj,
                methodReferences.ExceptionConstructor));

            newBodyInstructions.Add(ilProcessor.Create(OpCodes.Throw));

            return new TypesToAddToModuleAndNewBodyForAdaptation(typesToAdd.ToArray(), newBodyInstructions.ToArray());
        }
    }
}