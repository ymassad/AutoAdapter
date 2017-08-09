using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.InterfaceToInterfaceTests.GenericInterfaceToGenericInterfaceTest
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
            var adapter = CreateAdapter<ISourceInterface<string>, IDestinationInterface<string>>(new SourceClass());

            adapter.Echo("Input").Should().Be("Input");
        }
    }

    public interface ISourceInterface<T>
    {
        string Echo(T value);
    }

    public interface IDestinationInterface<T>
    {
        string Echo(T value);
    }

    public class SourceClass : ISourceInterface<string>
    {
        public string Echo(string value)
        {
            return value;
        }
    }
}