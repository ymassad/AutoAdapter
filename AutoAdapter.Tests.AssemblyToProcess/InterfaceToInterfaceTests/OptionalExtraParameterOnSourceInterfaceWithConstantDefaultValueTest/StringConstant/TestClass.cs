using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.InterfaceToInterfaceTests.OptionalExtraParameterOnSourceInterfaceWithConstantDefaultValueTest.StringConstant
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TSource, TDestination>(TSource instance)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<ISourceInterface, IDestinationInterface>(
                new SourceClass());

            adapter.Concat("Input").Should().Be("Inputdefault");
        }
    }

    public interface ISourceInterface
    {
        string Concat(string value, string extraParameter = "default");
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Concat(string value, string extraParameter)
        {
            return value + extraParameter;
        }
    }
}
