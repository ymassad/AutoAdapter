using System;
using System.Diagnostics;
using System.Linq.Expressions;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.BasicStaticMethodTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TSource, TDestination>(TSource instance)
        {
            throw new Exception();
        }

        [AdapterMethod]
        public TDestination CreateAdapter<TDestination>(Type @class, string methodName)
        {
            throw new Exception();
        }

        public void RunTest()
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
