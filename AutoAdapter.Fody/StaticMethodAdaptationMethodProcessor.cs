using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class StaticMethodAdaptationMethodProcessor : IAdaptationMethodProcessor<StaticMethodAdaptationMethod>
    {
        private readonly IStaticMethodToInterfaceAdapterFactory<StaticMethodToInterfaceAdaptationRequest> toInterfaceAdapterFactory;
        private readonly IStaticMethodAdaptationRequestsFinder<StaticMethodToInterfaceAdaptationRequest> toInterfaceAdaptationRequestsFinder;

        private readonly IStaticMethodToInterfaceAdapterFactory<StaticMethodToDelegateAdaptationRequest> toDelegateAdapterFactory;
        private readonly IStaticMethodAdaptationRequestsFinder<StaticMethodToDelegateAdaptationRequest> toDelegateAdaptationRequestsFinder;

        private readonly IReferenceImporter referenceImporter;

        public StaticMethodAdaptationMethodProcessor(
            IStaticMethodToInterfaceAdapterFactory<StaticMethodToInterfaceAdaptationRequest> toInterfaceAdapterFactory,
            IStaticMethodAdaptationRequestsFinder<StaticMethodToInterfaceAdaptationRequest> toInterfaceAdaptationRequestsFinder,
            IStaticMethodToInterfaceAdapterFactory<StaticMethodToDelegateAdaptationRequest> toDelegateAdapterFactory,
            IStaticMethodAdaptationRequestsFinder<StaticMethodToDelegateAdaptationRequest> toDelegateAdaptationRequestsFinder,
            IReferenceImporter referenceImporter)
        {
            this.toInterfaceAdapterFactory = toInterfaceAdapterFactory;
            this.toInterfaceAdaptationRequestsFinder = toInterfaceAdaptationRequestsFinder;
            this.referenceImporter = referenceImporter;
            this.toDelegateAdapterFactory = toDelegateAdapterFactory;
            this.toDelegateAdaptationRequestsFinder = toDelegateAdaptationRequestsFinder;
        }


        public class NewTypesAndNewInstructionsToAdd
        {
            public NewTypesAndNewInstructionsToAdd(TypeDefinition[] newTypes, Instruction[] newInstructions)
            {
                NewTypes = newTypes;
                NewInstructions = newInstructions;
            }

            public TypeDefinition[] NewTypes { get; }

            public Instruction[] NewInstructions { get; }
        }

        public TypesToAddToModuleAndNewBodyForAdaptation ProcessAdaptationMethod(
            ModuleDefinition module,
            StaticMethodAdaptationMethod adaptationMethod)
        {
            var method = adaptationMethod.Method;

            var ilProcessor = method.Body.GetILProcessor();

            var toInterfaceAdaptationResult = ProcessToInterfaceAdaptationRequests(module, ilProcessor, method);

            var newBody = new List<Instruction>();

            newBody.AddRange(toInterfaceAdaptationResult.NewInstructions);

            newBody.Add(ilProcessor.Create(OpCodes.Ldstr, "Adaptation request is not registered"));

            newBody.Add(ilProcessor.Create(
                OpCodes.Newobj,
                referenceImporter.ImportExceptionConstructor(module)));

            newBody.Add(ilProcessor.Create(OpCodes.Throw));

            return new TypesToAddToModuleAndNewBodyForAdaptation(toInterfaceAdaptationResult.NewTypes, newBody.ToArray());
        }

        private NewTypesAndNewInstructionsToAdd ProcessToInterfaceAdaptationRequests(
            ModuleDefinition module,
            ILProcessor ilProcessor,
            MethodDefinition method)
        {
            var getTypeFromHandleMethod = referenceImporter.ImportGetTypeFromHandleMethod(module);

            var typeEqualsMethod = referenceImporter.ImportTypeEqualsMethod(module);

            var stringEqualsMethod = referenceImporter.ImportStringEqualsMethod(module);

            var typesToAdd = new List<TypeDefinition>();

            var instructions = new List<Instruction>();

            var toInterfaceAdaptationRequests = toInterfaceAdaptationRequestsFinder.FindRequests(method);

            var toDelegateAdaptationRequests = toDelegateAdaptationRequestsFinder.FindRequests(method);

            foreach (var request in toInterfaceAdaptationRequests.Cast<StaticMethodAdaptationRequest>().Concat(toDelegateAdaptationRequests))
            {
                var adapterType = CreateAdapter(module, request);

                typesToAdd.Add(adapterType);

                var exitLabel = ilProcessor.Create(OpCodes.Nop);

                instructions.AddRange(
                    CreateInstructionsToCheckDestinationType(
                        ilProcessor,
                        method,
                        getTypeFromHandleMethod,
                        typeEqualsMethod,
                        exitLabel,
                        request));

                instructions.AddRange(
                    CreateInstructionstoCheckSourceStaticClass(
                        ilProcessor,
                        method,
                        request,
                        getTypeFromHandleMethod,
                        typeEqualsMethod,
                        exitLabel));

                instructions.AddRange(
                    CreateInstructionsToCheckSourceMethodName(
                        ilProcessor,
                        method,
                        request,
                        stringEqualsMethod,
                        exitLabel));

                instructions.AddRange(
                    CreateInstructionsToCheckExtraParametersObjectType(
                        module,
                        ilProcessor,
                        method,
                        request,
                        getTypeFromHandleMethod,
                        typeEqualsMethod,
                        exitLabel));

                instructions.AddRange(CreateInstructionsToLoadExtraParametersObjectArgument(ilProcessor, method, request));

                if (request is StaticMethodToInterfaceAdaptationRequest)
                {
                    instructions.AddRange(CreateInstructionsToCreateAdapterObject(ilProcessor, method, adapterType));
                }
                else
                {
                    var createDelegateMethod = referenceImporter.ImportMethodReference(module,
                        typeof(Delegate).GetMethod("CreateDelegate",
                            new[] { typeof(Type), typeof(object), typeof(string) }));

                    instructions.AddRange(
                        InstructionUtilities.CreateInstructionsForTypeOfOperator(
                            request.DestinationType,
                            ilProcessor,
                            getTypeFromHandleMethod));

                    instructions.Add(ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()));

                    instructions.Add(ilProcessor.Create(OpCodes.Ldstr, "Invoke"));

                    instructions.Add(ilProcessor.Create(OpCodes.Call, createDelegateMethod));

                    instructions.Add(ilProcessor.Create(OpCodes.Unbox_Any, method.GenericParameters[0]));
                }

                instructions.Add(ilProcessor.Create(OpCodes.Ret));

                instructions.Add(exitLabel);
            }

            return new NewTypesAndNewInstructionsToAdd(typesToAdd.ToArray(), instructions.ToArray());
        }

        private static List<Instruction> CreateInstructionsToCreateAdapterObject(
            ILProcessor ilProcessor,
            MethodDefinition method,
            TypeDefinition adapterType)
        {
            return new List<Instruction>
            {
                ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()),
                ilProcessor.Create(OpCodes.Unbox_Any, method.GenericParameters[0])
            };
        }

        private TypeDefinition CreateAdapter(ModuleDefinition module, StaticMethodAdaptationRequest request)
        {
            if (request is StaticMethodToInterfaceAdaptationRequest toInterfaceRequest)
            {
                return toInterfaceAdapterFactory.CreateAdapter(module, toInterfaceRequest);
            }

            if (request is StaticMethodToDelegateAdaptationRequest toDelegateRequest)
            {
                return toDelegateAdapterFactory.CreateAdapter(module, toDelegateRequest);
            }

            throw new Exception("Impossible");
        }

        private static List<Instruction> CreateInstructionsToLoadExtraParametersObjectArgument(
            ILProcessor ilProcessor,
            MethodDefinition method,
            StaticMethodAdaptationRequest request)
        {
            var instructions = new List<Instruction>();

            if (request.ExtraParametersObjectType.HasValue)
            {
                instructions.Add(
                    ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));

                instructions.Add(
                    ilProcessor.Create(OpCodes.Castclass, request.ExtraParametersObjectType.GetValue()));
            }
            return instructions;
        }

        private List<Instruction> CreateInstructionsToCheckExtraParametersObjectType(
            ModuleDefinition module,
            ILProcessor ilProcessor,
            MethodDefinition method,
            StaticMethodAdaptationRequest request,
            MethodReference getTypeFromHandleMethod,
            MethodReference typeEqualsMethod,
            Instruction exitLabel)
        {
            var instructions = new List<Instruction>();

            if (request.ExtraParametersObjectType.HasValue)
            {
                instructions.Add(
                    ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));

                instructions.Add(
                    ilProcessor.Create(OpCodes.Callvirt, referenceImporter.ImportGetTypeMethod(module)));

                instructions.AddRange(
                    InstructionUtilities.CreateInstructionsForTypeOfOperator(
                        request.ExtraParametersObjectType.GetValue(), ilProcessor, getTypeFromHandleMethod));

                instructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                instructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));
            }

            return instructions;
        }

        private static List<Instruction> CreateInstructionsToCheckSourceMethodName(
            ILProcessor ilProcessor,
            MethodDefinition method,
            StaticMethodAdaptationRequest request,
            MethodReference stringEqualsMethod,
            Instruction exitLabel)
        {
            var instructions = new List<Instruction>();

            var staticMethodNameParameterIndex =
                (request.ExtraParametersObjectType.HasValue ? 2 : 1) + (method.IsStatic ? 0 : 1);

            instructions.Add(ilProcessor.Create(OpCodes.Ldarg, staticMethodNameParameterIndex));

            instructions.Add(ilProcessor.Create(OpCodes.Ldstr, request.SourceStaticMethodName));

            instructions.Add(ilProcessor.Create(OpCodes.Callvirt, stringEqualsMethod));

            instructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

            return instructions;
        }

        private static List<Instruction> CreateInstructionstoCheckSourceStaticClass(
            ILProcessor ilProcessor,
            MethodDefinition method,
            StaticMethodAdaptationRequest request,
            MethodReference getTypeFromHandleMethod,
            MethodReference typeEqualsMethod,
            Instruction exitLabel)
        {
            var staticClassTypeParameterIndex =
                (request.ExtraParametersObjectType.HasValue ? 1 : 0) + (method.IsStatic ? 0 : 1);

            var instructions = new List<Instruction>();

            instructions.Add(ilProcessor.Create(OpCodes.Ldarg, staticClassTypeParameterIndex));

            instructions.AddRange(InstructionUtilities.CreateInstructionsForTypeOfOperator(
                request.SourceStaticClass, ilProcessor, getTypeFromHandleMethod));

            instructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

            instructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

            return instructions;
        }

        private static List<Instruction> CreateInstructionsToCheckDestinationType(
            ILProcessor ilProcessor,
            MethodDefinition method,
            MethodReference getTypeFromHandleMethod,
            MethodReference typeEqualsMethod,
            Instruction exitLabel,
            StaticMethodAdaptationRequest request)
        {
            var instructions = new List<Instruction>();

            instructions.AddRange(
                InstructionUtilities.CreateInstructionsForTypeOfOperator(
                    method.GenericParameters[0],
                    ilProcessor,
                    getTypeFromHandleMethod));

            instructions.AddRange(
                InstructionUtilities.CreateInstructionsForTypeOfOperator(
                    request.DestinationType,
                    ilProcessor,
                    getTypeFromHandleMethod));

            instructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

            instructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

            return instructions;
        }
    }
}
