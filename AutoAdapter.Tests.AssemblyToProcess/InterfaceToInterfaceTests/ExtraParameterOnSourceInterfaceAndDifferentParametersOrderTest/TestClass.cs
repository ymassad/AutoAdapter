using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.InterfaceToInterfaceTests.ExtraParameterOnSourceInterfaceAndDifferentParametersOrderTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TSource, TDestination>(TSource instance, object extraParameters)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<ISourceInterface, IDestinationInterface>(
                new SourceClass(),
                new {extraParameter = 1});

            adapter.Concat("Input").Should().Be("Input1");
        }
    }

    public interface ISourceInterface
    {
        string Concat(int extraParameter, string value);
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Concat(int extraParameter, string value)
        {
            return value + extraParameter;
        }
    }
}
