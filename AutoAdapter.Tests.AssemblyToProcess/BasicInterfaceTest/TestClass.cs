using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.BasicInterfaceTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TTo CreateAdapter<TFrom, TTo>(TFrom instance)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<ISourceInterface, IDestinationInterface>(new SourceClass());

            adapter.Echo("Input").Should().Be("Input");
        }
    }

    public interface ISourceInterface
    {
        string Echo(string value);
    }

    public interface IDestinationInterface
    {
        string Echo(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Echo(string value)
        {
            return value;
        }
    }
}
