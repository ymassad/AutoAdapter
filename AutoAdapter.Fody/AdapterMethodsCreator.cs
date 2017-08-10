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
            var targetMethodReturnType = targetAndSourceMethod.TargetMethod.MethodDefinition.ReturnType;

            if (targetMethodReturnType.IsGenericParameter)
            {
                var genericParameterIndex =
                    targetAndSourceMethod.TargetMethod.ReferencedType
                        .Resolve()
                        .GenericParameters
                        .Select((item, index) => new {item, index})
                        .Where(x => x.item.FullName == targetMethodReturnType.FullName)
                        .Select(x => x.index)
                        .First();

                targetMethodReturnType =
                    ((GenericInstanceType) targetAndSourceMethod.TargetMethod.ReferencedType).GenericArguments[genericParameterIndex];
            }

            var methodOnAdapter =
                new MethodDefinition(
                    targetAndSourceMethod.TargetMethod.MethodDefinition.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    targetMethodReturnType);

            foreach (var param in targetAndSourceMethod.TargetMethod.MethodDefinition.Parameters)
            {
                var parameterInformation = ParameterInformationExtractor.Extract(param, targetAndSourceMethod.TargetMethod.ReferencedType);

                var paramOnMethodOnAdapter =
                    new ParameterDefinition(parameterInformation.Name, parameterInformation.Attributes, parameterInformation.ParameterType);

                methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
            }

            var ilProcessor = methodOnAdapter.Body.GetILProcessor();

            ilProcessor.Emit(OpCodes.Ldarg_0);

            ilProcessor.Emit(OpCodes.Ldfld, adaptedField);

            var targetMethodParametersThatMatchSourceMethodParameters =
                targetAndSourceMethod.SourceMethod.MethodDefinition.Parameters
                    .Select(sourceParameter =>
                        new SourceAndTargetParameters(
                            ParameterInformationExtractor.Extract(sourceParameter, targetAndSourceMethod.SourceMethod.ReferencedType),
                            targetAndSourceMethod.TargetMethod.MethodDefinition
                                .Parameters
                                .FirstOrNoValue(p => p.Name == sourceParameter.Name)
                                .Chain(p => ParameterInformationExtractor.Extract(p, targetAndSourceMethod.TargetMethod.ReferencedType))))
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

            var sourceMethodToCall = targetAndSourceMethod.SourceMethod.MethodDefinition;

            if (sourceMethodToCall.Parameters.Any(x => x.ParameterType.IsGenericParameter) || sourceMethodToCall.ReturnType.IsGenericParameter)
            {
                var methodReferenceOnClosedGenericSourceType =
                    new MethodReference(
                        sourceMethodToCall.Name,
                        sourceMethodToCall.ReturnType,
                        targetAndSourceMethod.SourceMethod.ReferencedType)
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