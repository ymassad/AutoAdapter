using System;
using System.Linq;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.InterfaceToInterfaceTests.TwoMethodsInterfacesTest
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

            adapter.Echo("Input").Should().Be("Input");

            adapter.Reverse("Input").Should().Be("tupnI");
        }
    }

    public interface ISourceInterface
    {
        string Echo(string value);

        string Reverse(string value);
    }

    public interface IDestinationInterface
    {
        string Reverse(string value);

        string Echo(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Echo(string value)
        {
            return value;
        }

        public string Reverse(string value)
        {
            return new string(value.Reverse().ToArray());
        }
    }
}
