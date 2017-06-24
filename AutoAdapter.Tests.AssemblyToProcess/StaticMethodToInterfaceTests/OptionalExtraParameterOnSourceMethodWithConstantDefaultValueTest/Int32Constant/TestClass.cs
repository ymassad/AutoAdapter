using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.OptionalExtraParameterOnSourceMethodWithConstantDefaultValueTest.Int32Constant
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
            var adapter = CreateAdapter<IDestinationInterface>(
                typeof(SourceStaticClass),
                nameof(SourceStaticClass.Concat));

            adapter.Concat("Input").Should().Be("Input1");
        }
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public static class SourceStaticClass
    {
        public static string Concat(string value, int extraParameter = 1)
        {
            return value + extraParameter;
        }
    }
}
