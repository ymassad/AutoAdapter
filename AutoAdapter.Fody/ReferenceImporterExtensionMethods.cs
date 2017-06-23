using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public static class ReferenceImporterExtensionMethods
    {
        public static MethodReference ImportExceptionConstructor(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Exception).GetConstructor(new[] { typeof(string) }));
        }

        public static MethodReference ImportGetTypeMethod(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(object).GetMethod("GetType"));
        }

        public static MethodReference ImportTypeEqualsMethod(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Type).GetMethod("Equals", new[] { typeof(Type) }));
        }

        public static MethodReference ImportGetTypeFromHandleMethod(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(Type).GetMethod("GetTypeFromHandle"));
        }

        public static TypeReference ImportObjectType(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportTypeReference(module, typeof(object));
        }

        public static TypeReference ImportVoidType(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportTypeReference(module, typeof(void));
        }

        public static MethodReference ImportObjectConstructor(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(object).GetConstructor(new Type[0]));
        }

        public static MethodReference ImportStringEqualsMethod(this IReferenceImporter referenceImporter, ModuleDefinition module)
        {
            return referenceImporter.ImportMethodReference(module, typeof(string).GetMethod("Equals", new[] { typeof(string) }));
        }
    }
}
