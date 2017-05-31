using System;
using System.Collections.Generic;
using System.Linq;
using AutoAdapter.Tests.AssemblyToProcess;
using NUnit.Framework;

namespace AutoAdapter.Tests
{
    [TestFixture]
    public partial class WeaverTests
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

            var testClassType = newAssembly.GetType(assemblyName + "." + className + ".TestClass");

            object instance = testClassType.IsAbstract ? null : Activator.CreateInstance(testClassType);

            var runTestMethod = testClassType.GetMethod("RunTest");

            runTestMethod.Invoke(instance, new object[] { });
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