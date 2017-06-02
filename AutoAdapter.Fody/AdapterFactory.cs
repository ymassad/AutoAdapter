using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoAdapter.Fody
{
    public class AdapterFactory : IAdapterFactory
    {
        private readonly ModuleDefinition moduleDefinition;

        public AdapterFactory(ModuleDefinition moduleDefinition)
        {
            this.moduleDefinition = moduleDefinition;
        }

        public TypeDefinition CreateAdapter(AdaptationRequestInstance request)
        {
            if (!request.ToType.Resolve().IsInterface)
                throw new Exception("The destination type must be an interface");

            var adapterType = new TypeDefinition(null , "Adapter" + Guid.NewGuid(), TypeAttributes.Public, moduleDefinition.TypeSystem.Object);

            var adaptedField = new FieldDefinition("adapted", FieldAttributes.InitOnly | FieldAttributes.Private, request.FromType);

            adapterType.Fields.Add(adaptedField);

            Maybe<FieldDefinition> extraParametersField = Maybe<FieldDefinition>.NoValue();

            if (request.ExtraParametersType.HasValue)
            {
                extraParametersField =
                    new FieldDefinition(
                        "extraParameters",
                        FieldAttributes.InitOnly | FieldAttributes.Private,
                        request.ExtraParametersType.GetValue());

                adapterType.Fields.Add(extraParametersField.GetValue());
            }

            adapterType.Interfaces.Add(new InterfaceImplementation(request.ToType));

            CreateConstructor(request.FromType, adaptedField, extraParametersField, adapterType);

            CreateMethods(request, adaptedField, extraParametersField, adapterType);

            return adapterType;
        }

        private static void CreateMethods(
            AdaptationRequestInstance request,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField,
            TypeDefinition adapterType)
        {
            var resolvedToType = request.ToType.Resolve();
            var resolvedFromType = request.FromType.Resolve();

            var resolvedExtraParametersType = request.ExtraParametersType.Chain(x => x.Resolve());

            foreach (var targetMethod in resolvedToType.Methods)
            {
                var methodOnAdapter =
                    new MethodDefinition(
                        targetMethod.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        targetMethod.ReturnType);

                foreach (var param in targetMethod.Parameters)
                {
                    var paramOnMethodOnAdapter = new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);

                    methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
                }

                var methodOnAdapterIlProcessor = methodOnAdapter.Body.GetILProcessor();

                methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg_0);

                methodOnAdapterIlProcessor.Emit(OpCodes.Ldfld, adaptedField);

                var methodOnSourceType = resolvedFromType.Methods.Single(x => x.Name == targetMethod.Name);

                var targetMethodParametersThatMatchSourceMethodParameters =
                    methodOnSourceType.Parameters
                        .Select(x => new {SourceParameter = x, TargetParameter = targetMethod.Parameters.FirstOrDefault(p => p.Name == x.Name)})
                        .ToArray();

                targetMethodParametersThatMatchSourceMethodParameters
                    .ToList()
                    .ForEach(x =>
                    {
                        if (x.TargetParameter == null)
                        {
                            if(request.ExtraParametersType.HasNoValue)
                                throw new Exception("Expected ExtraParametersType to have a value");

                            methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg_0);

                            methodOnAdapterIlProcessor.Emit(OpCodes.Ldfld, extraParametersField.GetValue());
                            
                            var propertyOnExtraParametersObject =
                                resolvedExtraParametersType.GetValue().Properties
                                    .FirstOrDefault(p => p.Name == x.SourceParameter.Name);

                            if(propertyOnExtraParametersObject == null)
                                throw new Exception($"Could not find property {x.SourceParameter.Name} on the extra parameters object");

                            var propertyGetMethod = propertyOnExtraParametersObject.GetMethod;

                            var declaringType = request.ExtraParametersType.GetValue();

                            var returnType = propertyGetMethod.MethodReturnType.ReturnType;

                            MethodReference methodReference =
                                new MethodReference(
                                        propertyGetMethod.Name,
                                        returnType,
                                        declaringType)
                                    {HasThis = true};

                            methodOnAdapterIlProcessor.Emit(OpCodes.Callvirt, methodReference);
                        }
                        else
                        {
                            methodOnAdapterIlProcessor.Emit(OpCodes.Ldarg, x.SourceParameter.Index + 1);
                        } 
                    });

                methodOnAdapterIlProcessor.Emit(OpCodes.Callvirt, methodOnSourceType);

                methodOnAdapterIlProcessor.Emit(OpCodes.Ret);

                adapterType.Methods.Add(methodOnAdapter);
            }
        }

        private void CreateConstructor(
            TypeReference fromType,
            FieldDefinition field,
            Maybe<FieldDefinition> extraParametersField,
            TypeDefinition adapterType)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    moduleDefinition.TypeSystem.Void);

            constructor.Parameters.Add(new ParameterDefinition("adapted", ParameterAttributes.None, fromType));

            if (extraParametersField.HasValue)
            {
                constructor.Parameters.Add(new ParameterDefinition("extraParameters", ParameterAttributes.None, extraParametersField.GetValue().FieldType));
            }

            var objectConstructor =
                moduleDefinition.ImportReference(moduleDefinition.TypeSystem.Object.Resolve().GetConstructors().First());

            var processor = constructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, objectConstructor);

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Stfld, field);

            if (extraParametersField.HasValue)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_2);
                processor.Emit(OpCodes.Stfld, extraParametersField.GetValue());
            }

            processor.Emit(OpCodes.Ret);
            adapterType.Methods.Add(constructor);
        }
    }
}