using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Mono.Cecil;
using NUnit.Framework;

namespace AutoAdapter.Tests
{
    [TestFixture]
    public partial class WeaverTests
    {
        [Test]
        public void BasicInterfaceTest()
        {
            var result = StartTesting()
                .SetNamespace("AutoAdapter.Tests.AssemblyToProcess.BasicInterfaceTest")
                .InvokeAdapterMethod("TestInput");

            result.Should().Be("TestInput");
        }

        [Test]
        public void StaticCreateMethodTest()
        {
            var result = StartTesting()
                .SetNamespace("AutoAdapter.Tests.AssemblyToProcess.StaticCreateMethodTest")
                .TestClassIsStatic()
                .InvokeAdapterMethod("TestInput");

            result.Should().Be("TestInput");
        }

        [Test]
        public void ExtraParameterOnTargetInterfaceTest()
        {
            int extraParameterValue = 0;

            var result = StartTesting()
                .SetNamespace("AutoAdapter.Tests.AssemblyToProcess.ExtraParameterOnTargetInterfaceTest")
                .InvokeAdapterMethod("TestInput", extraParameterValue);

            result.Should().Be("TestInput");
        }

        private Fixture StartTesting() => new Fixture(newAssembly);

#if (DEBUG)
        [Test]
        public void PeVerify()
        {
            Verifier.Verify(assemblyPath, newAssemblyPath);
        }
#endif
    }
}