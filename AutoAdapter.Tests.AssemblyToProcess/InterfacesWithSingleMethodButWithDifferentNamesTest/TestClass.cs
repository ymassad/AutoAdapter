using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.InterfacesWithSingleMethodButWithDifferentNamesTest
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

            adapter.Echo2("Input").Should().Be("Input");
        }
    }

    public interface ISourceInterface
    {
        string Echo1(string value);
    }

    public interface IDestinationInterface
    {
        string Echo2(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Echo1(string value)
        {
            return value;
        }
    }
}
