using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.StaticCreateAdapterMethodTest
{
    public static class TestClass
    {
        //[AdapterMethod]
        public static TDestination CreateAdapter<TDestination>(Type @class, string methodName)
        {
            throw new Exception();
        }

        public static void RunTest()
        {
            var adapter = CreateAdapter<IDestinationInterface>(typeof(SourceStaticClass), nameof(SourceStaticClass.Echo));

            adapter.Echo("Input").Should().Be("Input");
        }
    }


    public interface IDestinationInterface
    {
        string Echo(string value);
    }

    public static class SourceStaticClass
    {
        public static string Echo(string value)
        {
            return value;
        }
    }
}
