using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody
{
    public class MembersCreatorForAdapterThatAdaptsFromStaticMethod : IMembersCreatorForAdapterThatAdaptsFromStaticMethod
    {
        private readonly ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument;

        private readonly IReferenceImporter referenceImporter;

        public MembersCreatorForAdapterThatAdaptsFromStaticMethod(
            ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument,
            IReferenceImporter referenceImporter)
        {
            this.creatorOfInsturctionsForArgument = creatorOfInsturctionsForArgument;
            this.referenceImporter = referenceImporter;
        }

        public MembersToAdd CreateMembers(
            ModuleDefinition module,
            SourceAndTargetMethods sourceAndTargetMethods,
            Maybe<TypeReference> extraParametersObjectType)
        {
            var extraParametersField =
                extraParametersObjectType.Chain(ExtraParametersObjectUtilities.CreateExtraParametersField);

            var methodOnAdapter = CreateAdapterMethod(sourceAndTargetMethods, extraParametersField);

            var constructor = CreateConstructor(module, extraParametersField);

            return new MembersToAdd(
                new[] { constructor, methodOnAdapter },
                new[] { extraParametersField }.GetValues().ToArray());
        }

        private MethodDefinition CreateAdapterMethod(
            SourceAndTargetMethods sourceAndTargetMethods,
            Maybe<FieldDefinition> extraParametersField)
        {
            var methodOnAdapter =
                new MethodDefinition(
                    sourceAndTargetMethods.TargetMethod.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    sourceAndTargetMethods.TargetMethod.ReturnType);

            foreach (var param in sourceAndTargetMethods.TargetMethod.Parameters)
            {
                var paramOnMethodOnAdapter =
                    new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);

                methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
            }

            var ilProcessor = methodOnAdapter.Body.GetILProcessor();

            var targetMethodParametersThatMatchSourceMethodParameters =
                sourceAndTargetMethods.SourceMethod.Parameters
                    .Select(sourceParam =>
                        new SourceAndTargetParameters(
                            ParameterInformationExtractor.Extract(sourceParam, sourceAndTargetMethods.SourceType),
                            sourceAndTargetMethods
                                .TargetMethod
                                .Parameters
                                .FirstOrNoValue(p => p.Name == sourceParam.Name)
                                .Chain(p => ParameterInformationExtractor.Extract(p, sourceAndTargetMethods.TargetType))))
                    .ToArray();

            targetMethodParametersThatMatchSourceMethodParameters
                .ToList()
                .ForEach(parameters =>
                {
                    var instructions =
                        creatorOfInsturctionsForArgument
                            .CreateInstructionsForArgument(
                                parameters,
                                ilProcessor,
                                extraParametersField);

                    ilProcessor.AppendRange(instructions);
                });

            ilProcessor.Emit(OpCodes.Call, sourceAndTargetMethods.SourceMethod);

            ilProcessor.Emit(OpCodes.Ret);

            return methodOnAdapter;
        }

        private MethodDefinition CreateConstructor(ModuleDefinition module, Maybe<FieldDefinition> extraParametersField)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    referenceImporter.ImportVoidType(module));

            extraParametersField.ExecuteIfHasValue(value =>
            {
                constructor.Parameters.Add(
                    new ParameterDefinition("extraParameters",
                        ParameterAttributes.None,
                        value.FieldType));
            });

            var processor = constructor.Body.GetILProcessor();

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, referenceImporter.ImportObjectConstructor(module));

            extraParametersField.ExecuteIfHasValue(value =>
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_1);
                processor.Emit(OpCodes.Stfld, value);
            });

            processor.Emit(OpCodes.Ret);

            return constructor;
        }
    }
}