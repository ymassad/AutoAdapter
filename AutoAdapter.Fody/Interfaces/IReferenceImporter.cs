using System;
using System.Reflection;
using Mono.Cecil;

namespace AutoAdapter.Fody.Interfaces
{
    public interface IReferenceImporter
    {
        TypeReference ImportTypeReference(ModuleDefinition module, Type type);
        MethodReference ImportMethodReference(ModuleDefinition module, MethodBase method);
    }
}