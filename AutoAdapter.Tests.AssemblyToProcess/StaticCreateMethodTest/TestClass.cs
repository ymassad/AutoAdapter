using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticCreateMethodTest
{
    public static class TestClass
    {
        //[AdapterMethod]
        public static TTo CreateAdapter<TFrom, TTo>(TFrom instance)
        {
            throw new Exception();
        }

        public static void RunTest()
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
