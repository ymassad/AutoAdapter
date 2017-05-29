using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

namespace AutoAdapter.Tests
{
    [TestFixture]
    public class WeaverTests
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

        [Test]
        public void BasicInterfaceTest()
        {
            //Arrrange
            var @namespace = "AutoAdapter.Tests.AssemblyToProcess.BasicInterfaceTest";

            var testClassType = newAssembly.GetType($"{@namespace}.Class");

            var instance = Activator.CreateInstance(testClassType);

            var createAdapterMethod = testClassType.GetMethod("CreateAdapter");

            var fromInterfaceType = newAssembly.GetType($"{@namespace}.IFromInterface");

            var fromClassType = newAssembly.GetType($"{@namespace}.FromClass");

            var fromClassInstance = Activator.CreateInstance(fromClassType);

            var toInterfaceType = newAssembly.GetType($"{@namespace}.IToInterface");

            var closedCreateAdapterMethod = createAdapterMethod.MakeGenericMethod(fromInterfaceType, toInterfaceType);

            //Act
            var adaptor = closedCreateAdapterMethod.Invoke(instance, new[]{fromClassInstance});

            var result = toInterfaceType.GetMethod("Echo").Invoke(adaptor, new object[] {"TestInput"});

            //Assert
            Assert.AreEqual("TestInput", result);
        }

        [Test]
        public void StaticCreateMethodTest()
        {
            //Arrrange
            var @namespace = "AutoAdapter.Tests.AssemblyToProcess.StaticCreateMethodTest";

            var testClassType = newAssembly.GetType($"{@namespace}.Class");

            var createAdapterMethod = testClassType.GetMethod("CreateAdapter");

            var fromInterfaceType = newAssembly.GetType($"{@namespace}.IFromInterface");

            var fromClassType = newAssembly.GetType($"{@namespace}.FromClass");

            var fromClassInstance = Activator.CreateInstance(fromClassType);

            var toInterfaceType = newAssembly.GetType($"{@namespace}.IToInterface");

            var closedCreateAdapterMethod = createAdapterMethod.MakeGenericMethod(fromInterfaceType, toInterfaceType);

            //Act
            var adaptor = closedCreateAdapterMethod.Invoke(null, new[] { fromClassInstance });

            var result = toInterfaceType.GetMethod("Echo").Invoke(adaptor, new object[] { "TestInput" });

            //Assert
            Assert.AreEqual("TestInput", result);
        }

#if (DEBUG)
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(assemblyPath, newAssemblyPath);
        }
#endif
    }
}