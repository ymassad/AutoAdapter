using System;
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

            var getTypeFromHandleMethod = ImportGetTypeFromHandleMethod(module);

            var typeEqualsMethod = ImportTypeEqualsMethod(module);

            foreach (var request in adaptationRequests)
            {
                var adapterType =
                    adapterFactory.CreateAdapter(module, request);

                typesToAdd.Add(adapterType);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, method.GenericParameters[0]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.SourceType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                var exitLabel = ilProcessor.Create(OpCodes.Nop);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, method.GenericParameters[1]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.DestinationType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                if (request.ExtraParametersObjectType.HasValue)
                {
                    newBodyInstructions.Add(ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, ImportGetTypeMethod(module) ));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.ExtraParametersObjectType.GetValue()));

                    newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

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
                ImportExceptionConstructor(module)));

            newBodyInstructions.Add(ilProcessor.Create(OpCodes.Throw));

            return new TypesToAddToModuleAndNewBodyForAdaptation(typesToAdd.ToArray(), newBodyInstructions.ToArray());
        }

        private MethodReference ImportExceptionConstructor(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Exception).GetConstructor(new[] { typeof(string) }));
        }

        private MethodReference ImportGetTypeMethod(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(object).GetMethod("GetType"));
        }

        private MethodReference ImportTypeEqualsMethod(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Type).GetMethod("Equals", new[] { typeof(Type) }));
        }

        private MethodReference ImportGetTypeFromHandleMethod(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Type).GetMethod("GetTypeFromHandle"));
        }
    }
}