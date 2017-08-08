using System.Collections.Generic;
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
            var resolvedDestinationType = request.DestinationType.Resolve();

            var resolvedSourceType = request.SourceType.Resolve();

            return sourceAndTargetMethodsMapper
                .CreateMap(resolvedDestinationType, resolvedSourceType)
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
                var paramOnMethodOnAdapter =
                    new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);

                methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
            }

            var ilProcessor = methodOnAdapter.Body.GetILProcessor();

            ilProcessor.Emit(OpCodes.Ldarg_0);

            ilProcessor.Emit(OpCodes.Ldfld, adaptedField);

            var targetMethodParametersThatMatchSourceMethodParameters =
                targetAndSourceMethod.SourceMethod.Parameters
                    .Select(x => new SourceAndTargetParameters(x,
                        targetAndSourceMethod.TargetMethod.Parameters.FirstOrNoValue(p => p.Name == x.Name)))
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

            ilProcessor.Emit(OpCodes.Callvirt, targetAndSourceMethod.SourceMethod);

            ilProcessor.Emit(OpCodes.Ret);

            return methodOnAdapter;
        }
    }
}