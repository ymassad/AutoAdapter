using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoAdapter.Fody.Interfaces;
using Mono.Cecil;

namespace AutoAdapter.Fody
{
    public class ReferenceImporter : IReferenceImporter
    {
        public TypeReference ImportTypeReference(ModuleDefinition module, Type type)
        {
            return module.ImportReference(type);
        }

        public MethodReference ImportMethodReference(ModuleDefinition module, MethodBase method)
        {
            return module.ImportReference(method);
        }
    }
}
