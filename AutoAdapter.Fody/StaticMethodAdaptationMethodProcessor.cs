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

        public TypesToAddToModuleAndNewBodyForAdaptationMethod ProcessAdaptationMethod(
            ModuleDefinition module,
            StaticMethodAdaptationMethod adaptationMethod)
        {
            var method = adaptationMethod.Method;

            var ilProcessor = method.Body.GetILProcessor();

            var toInterfaceAdaptationResult = ProcessAdaptationRequests(module, ilProcessor, method);

            var newBody = new List<Instruction>();

            newBody.AddRange(toInterfaceAdaptationResult.NewInstructions);

            newBody.Add(ilProcessor.Create(OpCodes.Ldstr, "Adaptation request is not registered"));

            newBody.Add(ilProcessor.Create(
                OpCodes.Newobj,
                referenceImporter.ImportExceptionConstructor(module)));

            newBody.Add(ilProcessor.Create(OpCodes.Throw));

            return new TypesToAddToModuleAndNewBodyForAdaptationMethod(toInterfaceAdaptationResult.NewTypes, newBody.ToArray());
        }

        private NewTypesAndNewInstructionsToAdd ProcessAdaptationRequests(
            ModuleDefinition module,
            ILProcessor ilProcessor,
            MethodDefinition adaptationMethod)
        {
            var getTypeFromHandleMethod = referenceImporter.ImportGetTypeFromHandleMethod(module);

            var typeEqualsMethod = referenceImporter.ImportTypeEqualsMethod(module);

            var stringEqualsMethod = referenceImporter.ImportStringEqualsMethod(module);

            var typesToAdd = new List<TypeDefinition>();

            var instructions = new List<Instruction>();

            var toInterfaceAdaptationRequests = toInterfaceAdaptationRequestsFinder.FindRequests(adaptationMethod);

            var toDelegateAdaptationRequests = toDelegateAdaptationRequestsFinder.FindRequests(adaptationMethod);

            var allAdaptationRequests =
                toInterfaceAdaptationRequests
                    .Cast<StaticMethodAdaptationRequest>()
                    .Concat(toDelegateAdaptationRequests)
                    .ToList();

            foreach (var request in allAdaptationRequests)
            {
                var adapterType = CreateAdapter(module, request);

                typesToAdd.Add(adapterType);

                var exitLabel = ilProcessor.Create(OpCodes.Nop);

                instructions.AddRange(
                    CreateInstructionsToCheckDestinationType(
                        ilProcessor,
                        adaptationMethod,
                        getTypeFromHandleMethod,
                        typeEqualsMethod,
                        exitLabel,
                        request));

                instructions.AddRange(
                    CreateInstructionstoCheckSourceStaticClass(
                        ilProcessor,
                        adaptationMethod,
                        request,
                        getTypeFromHandleMethod,
                        typeEqualsMethod,
                        exitLabel));

                instructions.AddRange(
                    CreateInstructionsToCheckSourceMethodName(
                        ilProcessor,
                        adaptationMethod,
                        request,
                        stringEqualsMethod,
                        exitLabel));

                instructions.AddRange(
                    CreateInstructionsToCheckExtraParametersObjectType(
                        module,
                        ilProcessor,
                        adaptationMethod,
                        request,
                        getTypeFromHandleMethod,
                        typeEqualsMethod,
                        exitLabel));

                instructions.AddRange(CreateInstructionsToLoadExtraParametersObjectArgument(ilProcessor, adaptationMethod, request));

                if (request is StaticMethodToInterfaceAdaptationRequest)
                {
                    instructions.Add(ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()));
                    instructions.Add(ilProcessor.Create(OpCodes.Unbox_Any, adaptationMethod.GenericParameters[0]));
                }
                else //StaticMethodToDelegate
                {
                    var createDelegateMethod =
                        referenceImporter
                            .ImportMethodReference(
                            module,
                            typeof(Delegate)
                                .GetMethod(
                                "CreateDelegate",
                                new[] { typeof(Type), typeof(object), typeof(string) }));

                    instructions.AddRange(
                        InstructionUtilities.CreateInstructionsForTypeOfOperator(
                            request.DestinationType,
                            ilProcessor,
                            getTypeFromHandleMethod));

                    instructions.Add(ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()));

                    instructions.Add(ilProcessor.Create(OpCodes.Ldstr, "Invoke"));

                    instructions.Add(ilProcessor.Create(OpCodes.Call, createDelegateMethod));

                    instructions.Add(ilProcessor.Create(OpCodes.Unbox_Any, adaptationMethod.GenericParameters[0]));
                }

                instructions.Add(ilProcessor.Create(OpCodes.Ret));

                instructions.Add(exitLabel);
            }

            return new NewTypesAndNewInstructionsToAdd(typesToAdd.ToArray(), instructions.ToArray());
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

        private static Instruction[] CreateInstructionsToLoadExtraParametersObjectArgument(
            ILProcessor ilProcessor,
            MethodDefinition adaptationMethod,
            StaticMethodAdaptationRequest request)
        {
            return request.ExtraParametersObjectType
                .Chain(type => new[]
                    {
                        ilProcessor.Create(adaptationMethod.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1),
                        ilProcessor.Create(OpCodes.Castclass, type)
                    })
                .GetValueOr(() => new Instruction[0]);
        }

        private List<Instruction> CreateInstructionsToCheckExtraParametersObjectType(
            ModuleDefinition module,
            ILProcessor ilProcessor,
            MethodDefinition adaptationMethod,
            StaticMethodAdaptationRequest request,
            MethodReference getTypeFromHandleMethod,
            MethodReference typeEqualsMethod,
            Instruction exitLabel)
        {
            var instructions = new List<Instruction>();

            if (request.ExtraParametersObjectType.HasValue)
            {
                instructions.Add(
                    ilProcessor.Create(adaptationMethod.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));

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
            MethodDefinition adaptationMethod,
            StaticMethodAdaptationRequest request,
            MethodReference stringEqualsMethod,
            Instruction exitLabel)
        {
            var instructions = new List<Instruction>();

            var staticMethodNameParameterIndex =
                (request.ExtraParametersObjectType.HasValue ? 2 : 1) + (adaptationMethod.IsStatic ? 0 : 1);

            instructions.Add(ilProcessor.Create(OpCodes.Ldarg, staticMethodNameParameterIndex));

            instructions.Add(ilProcessor.Create(OpCodes.Ldstr, request.SourceStaticMethodName));

            instructions.Add(ilProcessor.Create(OpCodes.Callvirt, stringEqualsMethod));

            instructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

            return instructions;
        }

        private static List<Instruction> CreateInstructionstoCheckSourceStaticClass(
            ILProcessor ilProcessor,
            MethodDefinition adaptationMethod,
            StaticMethodAdaptationRequest request,
            MethodReference getTypeFromHandleMethod,
            MethodReference typeEqualsMethod,
            Instruction exitLabel)
        {
            var staticClassTypeParameterIndex =
                (request.ExtraParametersObjectType.HasValue ? 1 : 0) + (adaptationMethod.IsStatic ? 0 : 1);

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
            MethodDefinition adaptationMethod,
            MethodReference getTypeFromHandleMethod,
            MethodReference typeEqualsMethod,
            Instruction exitLabel,
            StaticMethodAdaptationRequest request)
        {
            var instructions = new List<Instruction>();

            instructions.AddRange(
                InstructionUtilities.CreateInstructionsForTypeOfOperator(
                    adaptationMethod.GenericParameters[0],
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
