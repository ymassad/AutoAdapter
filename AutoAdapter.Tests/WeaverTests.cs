using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoAdapter.Fody;
using AutoAdapter.Tests.AssemblyToProcess;
using Mono.Cecil;
using NUnit.Framework;

namespace AutoAdapter.Tests
{
    [TestFixture]
    public class WeaverTests
    {
        public static IEnumerable<string> GetTestClasses()
        {
            var assembly = typeof(EmptyClass).Assembly;

            var assemblyName = assembly.GetName().Name;

            return assembly.GetTypes()
                .Where(x => x.Name == "TestClass")
                .Select(x => x.FullName.Substring(assemblyName.Length + 1))
                .Select(x => x.Substring(0, x.Length - ".TestClass".Length))
                .ToList();
        }

        [TestCaseSource(nameof(GetTestClasses))]
        public void RunTest(string className)
        {
            var assembly = typeof(EmptyClass).Assembly;

            var assemblyName = assembly.GetName().Name;

            var fullClassName = assemblyName + "." + className + ".TestClass";

            var assemblyInfo = CopyAndModifyAssembly(fullClassName);

            Verifier.Verify(assemblyInfo.OriginalAssemblyFilename, assemblyInfo.ModifiedAssemblyFilename);

            var testClassType = assemblyInfo.ModifiedAssembly.GetType(fullClassName);

            object instance = testClassType.IsAbstract ? null : Activator.CreateInstance(testClassType);

            var runTestMethod = testClassType.GetMethod("RunTest");

            runTestMethod.Invoke(instance, new object[] { });
        }

        public AssemblyInformation CopyAndModifyAssembly(string testClassName)
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
            var originalAssemblyFilename = Path.Combine(binariesPath, @"AutoAdapter.Tests.AssemblyToProcess.dll");

            var modifiedAssemblyFilename = Path.Combine(binariesPath, testClassName + ".dll");

            File.Copy(originalAssemblyFilename, modifiedAssemblyFilename, true);

            using (var moduleDefinition = ModuleDefinition.ReadModule(originalAssemblyFilename))
            {
                var adapterMethodAttributeClass =
                    moduleDefinition.Types.First(x => x.Name == "AdapterMethodAttribute");

                var testClass =
                    moduleDefinition.Types.First(x => x.FullName == testClassName);

                var createAdapterMethodOnTestClass =
                    testClass.Methods.First(x => x.Name == "CreateAdapter");

                var adapterMethodAttributeClassConstructor =
                    adapterMethodAttributeClass.Methods.First(x => x.IsConstructor);

                createAdapterMethodOnTestClass.CustomAttributes.Add(new CustomAttribute(adapterMethodAttributeClassConstructor));

                var weavingTask = new ModuleWeaver
                {
                    ModuleDefinition = moduleDefinition
                };

                weavingTask.Execute();
                moduleDefinition.Write(modifiedAssemblyFilename);
            }

            var modifiedAssembly = Assembly.LoadFile(modifiedAssemblyFilename);

            return new AssemblyInformation(modifiedAssembly, originalAssemblyFilename, modifiedAssemblyFilename);
        }
    }

    public class AssemblyInformation
    {
        public AssemblyInformation(Assembly modifiedAssembly, string originalAssemblyFilename, string modifiedAssemblyFilename)
        {
            ModifiedAssembly = modifiedAssembly;
            ModifiedAssemblyFilename = modifiedAssemblyFilename;
            OriginalAssemblyFilename = originalAssemblyFilename;
        }

        public Assembly ModifiedAssembly { get; }
        public string ModifiedAssemblyFilename { get; }
        public string OriginalAssemblyFilename { get; }
    }
}