using System;
using System.Linq;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class ModuleWeaver
    {
        public Action<string> LogInfo { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        public ModuleWeaver()
        {
            LogInfo = m => { };
        }

        private IAdapterFactory CreateAdapterFactory() =>
            new AdapterFactory(
                ModuleDefinition,
                new CreatorOfInsturctionsForArgument(),
                new SourceAndTargetMethodsMapper());

        private IAdaptationMethodsFinder CreateAdaptationMethodsFinder() => new AdaptationMethodsFinder();

        private IAdaptationRequestsFinder CreateAdaptationRequestsFinder() => new AdaptationRequestsFinder();

        public void Execute()
        {
            var adapterFactory = CreateAdapterFactory();

            var adaptationMethods = CreateAdaptationMethodsFinder().FindAdaptationMethods(ModuleDefinition);

            ModuleDefinition mscorlib = ModuleDefinition.ReadModule(typeof(object).Module.FullyQualifiedName);

            var typeDefinition = mscorlib.GetType("System.Type");

            var exceptionDefinition = mscorlib.GetType("System.Exception");

            var getTypeFromHandleMethod =
                ModuleDefinition
                    .ImportReference(typeDefinition.Methods.First(x => x.Name == "GetTypeFromHandle"));
            
            var equalsMethod =
                ModuleDefinition
                    .ImportReference(
                        typeDefinition.Methods
                            .First(
                                x => x.Name == "Equals"
                                && x.Parameters.Any()
                                && x.Parameters[0].ParameterType.FullName == "System.Type"));

            var exceptionConstructor =
                ModuleDefinition.ImportReference(exceptionDefinition.GetConstructors()
                    .First(x => x.Parameters.Count == 1 && x.Parameters[0].ParameterType.FullName == "System.String"));

            var getTypeMethod = ModuleDefinition
                .ImportReference(
                    mscorlib.GetType("System.Object")
                        .Methods.Single(x => x.Name == "GetType" && x.Parameters.Count == 0));

            var adaptationRequestsFinder = CreateAdaptationRequestsFinder();

            foreach (var adaptationMethod in adaptationMethods)
            {
                var adaptationRequests = adaptationRequestsFinder.FindRequests(adaptationMethod);

                adaptationMethod.Body.Instructions.Clear();

                var ilProcessor = adaptationMethod.Body.GetILProcessor();

                foreach (var request in adaptationRequests)
                {
                    var adapterType = adapterFactory.CreateAdapter(request);

                    ModuleDefinition.Types.Add(adapterType);

                    ilProcessor.Emit(OpCodes.Ldtoken, adaptationMethod.GenericParameters[0]);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Ldtoken, request.SourceType);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Callvirt, equalsMethod);

                    var exitLabel =  ilProcessor.Create(OpCodes.Nop);

                    ilProcessor.Emit(OpCodes.Brfalse, exitLabel);

                    ilProcessor.Emit(OpCodes.Ldtoken, adaptationMethod.GenericParameters[1]);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Ldtoken, request.DestinationType);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Callvirt, equalsMethod);

                    ilProcessor.Emit(OpCodes.Brfalse, exitLabel);

                    if (request.ExtraParametersObjectType.HasValue)
                    {
                        ilProcessor.Emit(adaptationMethod.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2);

                        ilProcessor.Emit(OpCodes.Callvirt, getTypeMethod);

                        ilProcessor.Emit(OpCodes.Ldtoken, request.ExtraParametersObjectType.GetValue());

                        ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                        ilProcessor.Emit(OpCodes.Callvirt, equalsMethod);

                        ilProcessor.Emit(OpCodes.Brfalse, exitLabel);
                    }

                    ilProcessor.Emit(adaptationMethod.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);

                    ilProcessor.Emit(OpCodes.Box, adaptationMethod.GenericParameters[0]);

                    ilProcessor.Emit(OpCodes.Castclass, request.SourceType);

                    if (request.ExtraParametersObjectType.HasValue)
                    {
                        ilProcessor.Emit(adaptationMethod.IsStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2);

                        ilProcessor.Emit(OpCodes.Castclass, request.ExtraParametersObjectType.GetValue());
                    }

                    ilProcessor.Emit(OpCodes.Newobj, adapterType.GetConstructors().First());

                    ilProcessor.Emit(OpCodes.Unbox_Any, adaptationMethod.GenericParameters[1]);

                    ilProcessor.Emit(OpCodes.Ret);

                    ilProcessor.Append(exitLabel);
                }

                ilProcessor.Emit(OpCodes.Ldstr, "Adaptation request is not registered");

                ilProcessor.Emit(
                    OpCodes.Newobj,
                    exceptionConstructor);

                ilProcessor.Emit(OpCodes.Throw);
            }
        }
    }
}
