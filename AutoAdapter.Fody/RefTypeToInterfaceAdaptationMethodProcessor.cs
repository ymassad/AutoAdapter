using System.Collections.Generic;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class RefTypeToInterfaceAdaptationMethodProcessor : IAdaptationMethodProcessor<RefTypeToInterfaceAdaptationMethod>
    {
        private readonly IAdapterFactory adapterFactory;
        private readonly IAdaptationRequestsFinder adaptationRequestsFinder;
        private readonly IReferenceImporter referenceImporter;

        public RefTypeToInterfaceAdaptationMethodProcessor(
            IAdapterFactory adapterFactory,
            IAdaptationRequestsFinder adaptationRequestsFinder,
            IReferenceImporter referenceImporter)
        {
            this.adapterFactory = adapterFactory;
            this.adaptationRequestsFinder = adaptationRequestsFinder;
            this.referenceImporter = referenceImporter;
        }

        public TypesToAddToModuleAndNewBodyForAdaptation ProcessAdaptationMethod(
            ModuleDefinition module,
            RefTypeToInterfaceAdaptationMethod adaptationMethod)
        {
            var method = adaptationMethod.Method;

            var typesToAdd = new List<TypeDefinition>();

            var newBodyInstructions = new List<Instruction>();

            var adaptationRequests = adaptationRequestsFinder.FindRequests(method);

            var ilProcessor = method.Body.GetILProcessor();

            var getTypeFromHandleMethod = referenceImporter.ImportGetTypeFromHandleMethod(module);

            var typeEqualsMethod = referenceImporter.ImportTypeEqualsMethod(module);

            Instruction[] CreateTypeOfInstructions(TypeReference type) =>
                InstructionUtilities.CreateInstructionsForTypeOfOperator(
                    type, ilProcessor, getTypeFromHandleMethod);

            foreach (var request in adaptationRequests)
            {
                var adapterType =
                    adapterFactory.CreateAdapter(module, request);

                typesToAdd.Add(adapterType);

                newBodyInstructions.AddRange(CreateTypeOfInstructions(method.GenericParameters[0]));
                
                newBodyInstructions.AddRange(CreateTypeOfInstructions(request.SourceType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                var exitLabel = ilProcessor.Create(OpCodes.Nop);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                newBodyInstructions.AddRange(CreateTypeOfInstructions(method.GenericParameters[1]));

                newBodyInstructions.AddRange(CreateTypeOfInstructions(request.DestinationType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                if (request.ExtraParametersObjectType.HasValue)
                {
                    newBodyInstructions.Add(ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, referenceImporter.ImportGetTypeMethod(module) ));

                    newBodyInstructions.AddRange(CreateTypeOfInstructions(request.ExtraParametersObjectType.GetValue()));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));
                }

                newBodyInstructions.Add(ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Box, method.GenericParameters[0]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Castclass, request.SourceType));

                if (request.ExtraParametersObjectType.HasValue)
                {
                    newBodyInstructions.Add(ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Castclass, request.ExtraParametersObjectType.GetValue()));
                }

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Unbox_Any, method.GenericParameters[1]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ret));

                newBodyInstructions.Add(exitLabel);
            }

            newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldstr, "Adaptation request is not registered"));

            newBodyInstructions.Add(ilProcessor.Create(
                OpCodes.Newobj,
                referenceImporter.ImportExceptionConstructor(module)));

            newBodyInstructions.Add(ilProcessor.Create(OpCodes.Throw));

            return new TypesToAddToModuleAndNewBodyForAdaptation(typesToAdd.ToArray(), newBodyInstructions.ToArray());
        }
    }
}