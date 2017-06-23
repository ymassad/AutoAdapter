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
    public class StaticMethodToInterfaceAdapterFactory : IStaticMethodAdapterFactory
    {
        private readonly ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument;

        private readonly IReferenceImporter referenceImporter;

        public StaticMethodToInterfaceAdapterFactory(IReferenceImporter referenceImporter, ICreatorOfInsturctionsForArgument creatorOfInsturctionsForArgument)
        {
            this.referenceImporter = referenceImporter;
            this.creatorOfInsturctionsForArgument = creatorOfInsturctionsForArgument;
        }

        public TypeDefinition CreateAdapter(
            ModuleDefinition module,
            StaticMethodAdaptationRequestInstance request)
        {
            var resolvedDestinationType = request.DestinationType.Resolve();

            if (!resolvedDestinationType.IsInterface)
                throw new Exception("The destination type must be an interface");

            var destinationMethods = resolvedDestinationType.GetMethods().ToArray();
            
            if(destinationMethods.Length != 1)
                throw new Exception("Target interface should contain only one method");

            var resolvedSourceClass = request.SourceStaticClass.Resolve();

            var sourceClassMethods = resolvedSourceClass.GetMethods().ToArray();

            var sourceMethodsWithRequestedName = sourceClassMethods
                .Where(x => x.Name.Equals(request.SourceStaticMethodName)).ToArray();

            if(sourceMethodsWithRequestedName.Length == 0)
                throw new Exception("Could not find requested method in source static class");

            var sourceMethod = sourceMethodsWithRequestedName.First();

            var destinationMethod = destinationMethods[0];

            var adapterType = new TypeDefinition(
                null, "Adapter" + Guid.NewGuid(), TypeAttributes.Public, referenceImporter.ImportObjectType(module));

            adapterType.Interfaces.Add(new InterfaceImplementation(request.DestinationType));
            
            var methodOnAdapter =
                new MethodDefinition(
                    destinationMethod.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    destinationMethod.ReturnType);

            foreach (var param in destinationMethod.Parameters)
            {
                var paramOnMethodOnAdapter =
                    new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);

                methodOnAdapter.Parameters.Add(paramOnMethodOnAdapter);
            }

            var ilProcessor = methodOnAdapter.Body.GetILProcessor();


            var targetMethodParametersThatMatchSourceMethodParameters =
                sourceMethod.Parameters
                    .Select(x => new SourceAndTargetParameters(x,
                        destinationMethod.Parameters.FirstOrNoValue(p => p.Name == x.Name)))
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
                                request.ExtraParametersObjectType,
                                Maybe<FieldDefinition>.NoValue());

                    ilProcessor.AppendRange(instructions);
                });

            ilProcessor.Emit(OpCodes.Call, sourceMethod);

            ilProcessor.Emit(OpCodes.Ret);

            adapterType.Methods.Add(methodOnAdapter);

            var constructor = CreateConstructor(module);

            adapterType.Methods.Add(constructor);

            return adapterType;
        }

        private MethodDefinition CreateConstructor(ModuleDefinition module)
        {
            var constructor =
                new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    referenceImporter.ImportVoidType(module));

            var processor = constructor.Body.GetILProcessor();

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, referenceImporter.ImportObjectConstructor(module));

            processor.Emit(OpCodes.Ret);

            return constructor;
        }
    }
}
