using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.InterfaceToInterfaceTests.GenericInterfaceToGenericInterfaceWhereReturnTypeIsGenericTest
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
        T Echo(string value);
    }

    public interface IDestinationInterface<T>
    {
        T Echo(string value);
    }

    public class SourceClass : ISourceInterface<string>
    {
        public string Echo(string value)
        {
            return value;
        }
    }
}