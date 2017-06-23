using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.ExtraParameterOnTargetInterfaceTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TDestination>(Type @class, string methodName)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<IDestinationInterface>(typeof(SourceStaticClass), nameof(SourceStaticClass.Echo));

            int extraParameter = 1;

            adapter.Echo("value", extraParameter).Should().Be("value");
        }
    }

    public interface IDestinationInterface
    {
        string Echo(string value, int extraParameter);
    }

    public static class SourceStaticClass
    {
        public static string Echo(string value)
        {
            return value;
        }
    }
}
