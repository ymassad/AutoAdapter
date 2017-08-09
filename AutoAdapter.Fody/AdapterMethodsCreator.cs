using System.Linq;
using AutoAdapter.Fody.DTOs;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoAdapter.Fody
{
    public class AdapterMethodsCreator : IAdapterMethodsCreator
    {
        private readonly ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument;
        private readonly ISourceAndTargetMethodsMapper sourceAndTargetMethodsMapper;

        public AdapterMethodsCreator(
            ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument,
            ISourceAndTargetMethodsMapper sourceAndTargetMethodsMapper)
        {
            this.creatorOfInsturctionsForArgument = creatorOfInsturctionsForArgument;
            this.sourceAndTargetMethodsMapper = sourceAndTargetMethodsMapper;
        }

        public MethodDefinition[] CreateAdapterMethods(
            RefTypeToInterfaceAdaptationRequest request,
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField)
        {

            return sourceAndTargetMethodsMapper
                .CreateMap(request.DestinationType, request.SourceType)
                .Select(targetAndSourceMethod =>
                    CreateAdapterMethod(adaptedField, extraParametersField, targetAndSourceMethod))
                .ToArray();
        }

        private MethodDefinition CreateAdapterMethod(
            FieldDefinition adaptedField,
            Maybe<FieldDefinition> extraParametersField,
            SourceAndTargetMethods targetAndSourceMethod)
        {
            var methodOnAdapter =
                new MethodDefinition(
                    targetAndSourceMethod.TargetMethod.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    targetAndSourceMethod.TargetMethod.ReturnType);

            foreach (var param in targetAndSourceMethod.TargetMethod.Parameters)
            {
                var parameterInformation = ParameterInformationExtractor.Extract(param, targetAndSourceMethod.TargetType);

                var paramOnMethodOnAdapter =
                    new ParameterDefinition(parameterInformation.Name, parameterInformation.Attributes, parameterInformation.ParameterType);

                methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
            }

            var ilProcessor = methodOnAdapter.Body.GetILProcessor();

            ilProcessor.Emit(OpCodes.Ldarg_0);

            ilProcessor.Emit(OpCodes.Ldfld, adaptedField);

            var targetMethodParametersThatMatchSourceMethodParameters =
                targetAndSourceMethod.SourceMethod.Parameters
                    .Select(sourceParameter =>
                        new SourceAndTargetParameters(
                            ParameterInformationExtractor.Extract(sourceParameter, targetAndSourceMethod.SourceType),
                            targetAndSourceMethod
                                .TargetMethod
                                .Parameters
                                .FirstOrNoValue(p => p.Name == sourceParameter.Name)
                                .Chain(p => ParameterInformationExtractor.Extract(p, targetAndSourceMethod.TargetType))))
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

            var sourceMethodToCall = targetAndSourceMethod.SourceMethod;

            if (sourceMethodToCall.Parameters.Any(x => x.ParameterType.IsGenericParameter))
            {
                var methodReferenceOnClosedGenericSourceType =
                    new MethodReference(
                        sourceMethodToCall.Name,
                        sourceMethodToCall.ReturnType,
                        targetAndSourceMethod.SourceType)
                {
                    HasThis = sourceMethodToCall.HasThis,
                    CallingConvention = sourceMethodToCall.CallingConvention,
                    ExplicitThis = sourceMethodToCall.ExplicitThis
                };

                methodReferenceOnClosedGenericSourceType.Parameters.AddRange(sourceMethodToCall.Parameters);

                ilProcessor.Emit(OpCodes.Callvirt, methodReferenceOnClosedGenericSourceType);
            }
            else
            {
                ilProcessor.Emit(OpCodes.Callvirt, sourceMethodToCall);
            }

            ilProcessor.Emit(OpCodes.Ret);

            return methodOnAdapter;
        }
    }
}