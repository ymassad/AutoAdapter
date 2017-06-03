using FluentAssertions;
using System;

namespace AutoAdapter.Tests.AssemblyToProcess.ExtraParameterOnSourceInterfaceTest
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
        string Concat(string value, int extraParameter);
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Concat(string value, int extraParameter)
        {
            return value + extraParameter;
        }
    }
}
