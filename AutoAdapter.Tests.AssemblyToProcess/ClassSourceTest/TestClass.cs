using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.ClassSourceTest
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
            var adapter = CreateAdapter<SourceClass, IDestinationInterface>(new SourceClass());

            adapter.Echo("Input").Should().Be("Input");
        }
    }

    public interface IDestinationInterface
    {
        string Echo(string value);
    }

    public class SourceClass
    {
        public string Echo(string value)
        {
            return value;
        }
    }
}
