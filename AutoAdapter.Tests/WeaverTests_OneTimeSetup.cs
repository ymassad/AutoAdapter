using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using NUnit.Framework;

namespace AutoAdapter.Tests
{
    public partial class WeaverTests
    {
        private Assembly newAssembly;
        private string newAssemblyPath;
        private string assemblyPath;

        [OneTimeSetUp]
        public void Setup()
        {
            var projectDirectory
                = Path.GetDirectoryName(Path.GetFullPath(
                    Path.Combine(
                        TestContext.CurrentContext.TestDirectory,
                        @"..\..\..\AutoAdapter.Tests.AssemblyToProcess\AutoAdapter.Tests.AssemblyToProcess.csproj")));

            var binariesPath = Path.Combine(projectDirectory, @"bin\Debug");

#if (!DEBUG)
        binariesPath = binariesPath.Replace("Debug", "Release");
#endif

            var modifiedBinariesPath = Path.Combine(binariesPath, "Modified");

            if (!Directory.Exists(modifiedBinariesPath))
                Directory.CreateDirectory(modifiedBinariesPath);

            assemblyPath = Path.Combine(binariesPath, @"AutoAdapter.Tests.AssemblyToProcess.dll");

            newAssemblyPath = Path.Combine(modifiedBinariesPath, @"AutoAdapter.Tests.AssemblyToProcess.dll");

            File.Copy(assemblyPath, newAssemblyPath, true);

            using (var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath))
            {
                var weavingTask = new ModuleWeaver
                {
                    ModuleDefinition = moduleDefinition
                };

                weavingTask.Execute();
                moduleDefinition.Write(newAssemblyPath);
            }

            newAssembly = Assembly.LoadFile(newAssemblyPath);
        }
    }
}
