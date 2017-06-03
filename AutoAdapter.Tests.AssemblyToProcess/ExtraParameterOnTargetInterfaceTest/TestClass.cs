using FluentAssertions;
using System;

namespace AutoAdapter.Tests.AssemblyToProcess.ExtraParameterOnTargetInterfaceTest
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
            var adapter = CreateAdapter<ISourceInterface, IDestinationInterface>(new SourceClass());

            var extraParameter = 0;

            adapter.Echo("Input", extraParameter).Should().Be("Input");
        }
    }

    public interface ISourceInterface
    {
        string Echo(string value);
    }

    public interface IDestinationInterface
    {
        string Echo(string value, int extraParameter);
    }

    public class SourceClass : ISourceInterface
    {
        public string Echo(string value)
        {
            return value;
        }
    }
}
