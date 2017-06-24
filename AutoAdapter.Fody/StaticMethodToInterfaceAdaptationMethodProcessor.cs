using System.Collections.Generic;
using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodToInterfaceAdaptationMethodProcessor : IAdaptationMethodProcessor<StaticMethodToInterfaceAdaptationMethod>
    {
        private readonly IStaticMethodAdapterFactory adapterFactory;
        private readonly IStaticAdaptationRequestsFinder adaptationRequestsFinder;
        private readonly IReferenceImporter referenceImporter;

        public StaticMethodToInterfaceAdaptationMethodProcessor(
            IStaticMethodAdapterFactory adapterFactory,
            IStaticAdaptationRequestsFinder adaptationRequestsFinder,
            IReferenceImporter referenceImporter)
        {
            this.adapterFactory = adapterFactory;
            this.adaptationRequestsFinder = adaptationRequestsFinder;
            this.referenceImporter = referenceImporter;
        }

        public TypesToAddToModuleAndNewBodyForAdaptation ProcessAdaptationMethod(
            ModuleDefinition module,
            StaticMethodToInterfaceAdaptationMethod adaptationMethod)
        {
            var method = adaptationMethod.Method;

            var typesToAdd = new List<TypeDefinition>();

            var newBodyInstructions = new List<Instruction>();

            var adaptationRequests = adaptationRequestsFinder.FindRequests(method);

            var ilProcessor = method.Body.GetILProcessor();

            var getTypeFromHandleMethod = referenceImporter.ImportGetTypeFromHandleMethod(module);

            var typeEqualsMethod = referenceImporter.ImportTypeEqualsMethod(module);

            var stringEqualsMethod = referenceImporter.ImportStringEqualsMethod(module);

            Instruction[] CreateTypeOfInstructions(TypeReference type) =>
                InstructionUtilities.CreateInstructionsForTypeOfOperator(
                    type, ilProcessor, getTypeFromHandleMethod);

            foreach (var request in adaptationRequests)
            {
                var adapterType =
                    adapterFactory.CreateAdapter(module, request);

                typesToAdd.Add(adapterType);

                newBodyInstructions.AddRange(CreateTypeOfInstructions(method.GenericParameters[0]));

                newBodyInstructions.AddRange(CreateTypeOfInstructions(request.DestinationType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                var exitLabel = ilProcessor.Create(OpCodes.Nop);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                var staticClassTypeParameterIndex =
                    (request.ExtraParametersObjectType.HasValue ? 1 : 0) + (method.IsStatic ? 0 : 1);
                var staticMethodNameParameterIndex =
                    (request.ExtraParametersObjectType.HasValue ? 2 : 1) + (method.IsStatic ? 0 : 1);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldarg, staticClassTypeParameterIndex));

                newBodyInstructions.AddRange(CreateTypeOfInstructions(request.SourceStaticClass));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldarg, staticMethodNameParameterIndex));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldstr , request.SourceStaticMethodName));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, stringEqualsMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                if (request.ExtraParametersObjectType.HasValue)
                {
                    newBodyInstructions.Add(ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Castclass, request.ExtraParametersObjectType.GetValue()));
                }

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Unbox_Any, method.GenericParameters[0]));

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
