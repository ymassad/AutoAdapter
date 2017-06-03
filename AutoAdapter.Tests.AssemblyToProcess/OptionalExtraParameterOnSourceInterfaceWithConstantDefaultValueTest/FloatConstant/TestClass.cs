using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.OptionalExtraParameterOnSourceInterfaceWithConstantDefaultValueTest.FloatConstant
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

            adapter.Concat("Input").Should().Be("Input1.1");
        }
    }

    public interface ISourceInterface
    {
        string Concat(string value, float extraParameter = 1.1f);
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Concat(string value, float extraParameter)
        {
            return value + extraParameter;
        }
    }
}
