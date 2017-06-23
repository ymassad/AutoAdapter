using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var getTypeFromHandleMethod = ImportGetTypeFromHandleMethod(module);

            var typeEqualsMethod = ImportTypeEqualsMethod(module);

            var stringEqualsMethod = ImportStringEqualsMethod(module);

            foreach (var request in adaptationRequests)
            {
                var adapterType =
                    adapterFactory.CreateAdapter(module, request);

                typesToAdd.Add(adapterType);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, method.GenericParameters[0]));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.DestinationType));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                var exitLabel = ilProcessor.Create(OpCodes.Nop);

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                newBodyInstructions.Add(ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldtoken, request.SourceStaticClass));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Call, getTypeFromHandleMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, typeEqualsMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                newBodyInstructions.Add(ilProcessor.Create(method.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Ldstr , request.SourceStaticMethodName));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Callvirt, stringEqualsMethod));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Brfalse, exitLabel));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Newobj, adapterType.GetConstructors().First()));

                newBodyInstructions.Add(ilProcessor.Create(OpCodes.Unbox_Any, method.GenericParameters[0]));

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

        private MethodReference ImportTypeEqualsMethod(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Type).GetMethod("Equals", new[] { typeof(Type) }));
        }

        private MethodReference ImportStringEqualsMethod(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(string).GetMethod("Equals", new[] { typeof(string) }));
        }

        private MethodReference ImportGetTypeFromHandleMethod(ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Type).GetMethod("GetTypeFromHandle"));
        }
    }
}
