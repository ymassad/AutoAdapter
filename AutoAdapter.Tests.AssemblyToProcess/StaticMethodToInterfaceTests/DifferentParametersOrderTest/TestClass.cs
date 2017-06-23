using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.DifferentParametersOrderTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TDestination>(Type @class, string methodName)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<IDestinationInterface>(typeof(SourceStaticClass), nameof(SourceStaticClass.Concat));

            adapter.Concat("second", "first").Should().Be("firstsecond");
        }
    }

    public interface IDestinationInterface
    {
        string Concat(string second, string first);
    }

    public static class SourceStaticClass
    {
        public static string Concat(string first, string second)
        {
            return first + second;
        }
    }
}
