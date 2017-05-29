using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter
{
    public class ModuleWeaver
    {
        public Action<string> LogInfo { get; set; }

        public ModuleDefinition ModuleDefinition { get; set; }

        public ModuleWeaver()
        {
            LogInfo = m => { };
        }

        private IAdapterFactory CreateAdapterFactory() => new AdapterFactory(ModuleDefinition);

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
                    var adapterType = adapterFactory.CreateAdapter(request.FromType, request.ToType);

                    ModuleDefinition.Types.Add(adapterType);

                    ilProcessor.Emit(OpCodes.Ldtoken, adaptationMethod.GenericParameters[0]);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Ldtoken, request.FromType);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Callvirt, equalsMethod);

                    var exitWithErrorLabel =  ilProcessor.Create(OpCodes.Nop);

                    ilProcessor.Emit(OpCodes.Brfalse, exitWithErrorLabel);

                    ilProcessor.Emit(OpCodes.Ldtoken, adaptationMethod.GenericParameters[1]);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Ldtoken, request.ToType);

                    ilProcessor.Emit(OpCodes.Call, getTypeFromHandleMethod);

                    ilProcessor.Emit(OpCodes.Callvirt, equalsMethod);

                    ilProcessor.Emit(OpCodes.Brfalse, exitWithErrorLabel);

                    ilProcessor.Emit(adaptationMethod.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);

                    ilProcessor.Emit(OpCodes.Newobj, adapterType.GetConstructors().First());
                    
                    ilProcessor.Emit(OpCodes.Ret);

                    ilProcessor.Append(exitWithErrorLabel);

                    ilProcessor.Emit(OpCodes.Ldstr, "Cannot convert from " + request.FromType.Name + " to " + request.ToType.Name);

                    ilProcessor.Emit(
                        OpCodes.Newobj,
                        exceptionConstructor);

                    ilProcessor.Emit(OpCodes.Throw);
                }
            }
        }

        public class AdaptationRequestInstance
        {
            public AdaptationRequestInstance(TypeDefinition fromType, TypeDefinition toType)
            {
                FromType = fromType;
                ToType = toType;
            }

            public TypeDefinition FromType { get; }
            public TypeDefinition ToType { get; }
        }

        public AdaptationRequestInstance[] GetAdaptationRequests(MethodDefinition adaptationMethod)
        {
            return ModuleDefinition
                .GetTypes()
                .SelectMany(x => x.GetMethods())
                .SelectMany(x => GetInstructionInMethodThatCallOtherMethod(x, adaptationMethod))
                .Select(x => (GenericInstanceMethod) x.Operand)
                .Select(x => x.GenericArguments)
                .Select(x => new AdaptationRequestInstance(x[0].Resolve(), x[1].Resolve()))
                .ToArray();
        }

        private Instruction[] GetInstructionInMethodThatCallOtherMethod(
            MethodDefinition methodToSearch,
            MethodDefinition calledMethod)
        {
            if (!methodToSearch.HasBody)
                return new Instruction[0];

            return
                methodToSearch
                    .Body
                    .Instructions
                    .Where(x => x.OpCode == OpCodes.Call)
                    .Where(x => x.Operand is GenericInstanceMethod)
                    .Where(x => ((GenericInstanceMethod) x.Operand).ElementMethod == calledMethod)
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
