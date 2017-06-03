using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.DifferentParametersOrderTest
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

            adapter.Concat("second", "first").Should().Be("firstsecond");
        }
    }

    public interface ISourceInterface
    {
        string Concat(string first, string second);
    }

    public interface IDestinationInterface
    {
        string Concat(string second, string first);
    }

    public class SourceClass : ISourceInterface
    {
        public string Concat(string first, string second)
        {
            return first + second;
        }
    }
}
