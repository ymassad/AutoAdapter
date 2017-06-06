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

        public void Execute()
        {
            var adapterFactory = CreateAdapterFactory();

            var adaptationMethods = GetAdaptationMethods();

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

            foreach (var adaptationMethod in adaptationMethods)
            {
                var adaptationRequests = GetAdaptationRequests(adaptationMethod);

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

                    var exitWithErrorLabel =  ilProcessor.Create(OpCodes.Nop);

                    ilProcessor.Emit(OpCodes.Brfalse, exitWithErrorLabel);

                    ilProcessor.Emit(OpCodes.Ldtoken, adaptationMethod.GenericParameters[1]);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Ldtoken, request.DestinationType);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Callvirt, equalsMethod);

                    ilProcessor.Emit(OpCodes.Brfalse, exitWithErrorLabel);

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

                    ilProcessor.Append(exitWithErrorLabel);

                    ilProcessor.Emit(OpCodes.Ldstr, "Cannot convert from " + request.SourceType.Name + " to " + request.DestinationType.Name);

                    ilProcessor.Emit(
                        OpCodes.Newobj,
                        exceptionConstructor);

                    ilProcessor.Emit(OpCodes.Throw);
                }
            }
        }

        public AdaptationRequestInstance[] GetAdaptationRequests(MethodDefinition adaptationMethod)
        {
            return ModuleDefinition
                .GetTypes()
                .SelectMany(x => x.GetMethods())
                .SelectMany(x =>
                    GetInstructionsInMethodThatCallSomeGenericMethod(
                        x,
                        adaptationMethod)
                        .Select(index => new {Method = x, InstructionIndex = index}))
                .Select(x => CreateAdaptationRequestForInstruction(x.Method, x.InstructionIndex))
                .ToArray();
        }

        private AdaptationRequestInstance CreateAdaptationRequestForInstruction(
            MethodDefinition methodToSearch,
            int instructionIndex)
        {
            var bodyInstructions = methodToSearch.Body.Instructions;

            var instruction = bodyInstructions[instructionIndex];

            var genericInstanceMethod = (GenericInstanceMethod) instruction.Operand;

            if (genericInstanceMethod.Parameters.Count == 1)
            {
                return new AdaptationRequestInstance(
                    genericInstanceMethod.GenericArguments[0],
                    genericInstanceMethod.GenericArguments[1]);
            }
            else
            {
                var previousInstruction = bodyInstructions[instructionIndex - 1];

                if(previousInstruction.OpCode != OpCodes.Newobj)
                    throw new Exception("Uexpected to find a Newobj instruction");

                MethodReference constructor = (MethodReference)previousInstruction.Operand;

                return new AdaptationRequestInstance(
                    genericInstanceMethod.GenericArguments[0],
                    genericInstanceMethod.GenericArguments[1],
                    Maybe<TypeReference>.OfValue(constructor.DeclaringType));
            }
        }


        private int[] GetInstructionsInMethodThatCallSomeGenericMethod(
            MethodDefinition methodToSearch,
            MethodDefinition calledMethod)
        {
            if (!methodToSearch.HasBody)
                return new int[0];

            return
                methodToSearch
                    .Body
                    .Instructions
                    .Select((x,i) => (Instruction: x, Index: i))
                    .Where(x => x.Instruction.OpCode == OpCodes.Call)
                    .Where(x => x.Instruction.Operand is GenericInstanceMethod)
                    .Where(x => ((GenericInstanceMethod) x.Instruction.Operand).ElementMethod == calledMethod)
                    .Select(x => x.Index)
                    .ToArray();
        }

        private MethodDefinition[] GetAdaptationMethods()
        {
            return ModuleDefinition
                .GetTypes()
                .SelectMany(x => x.GetMethods())
                .Where(x => x.Parameters.Count > 0)
                .Where(x => x.GenericParameters.Count == 2)
                .Where(x => x.ReturnType == x.GenericParameters[1])
                .Where(x => x.Parameters[0].ParameterType == x.GenericParameters[0])
                .Where(x => x.CustomAttributes.Any(a => a.AttributeType.Name == "AdapterMethodAttribute"))
                .ToArray();
        }
    }
}
