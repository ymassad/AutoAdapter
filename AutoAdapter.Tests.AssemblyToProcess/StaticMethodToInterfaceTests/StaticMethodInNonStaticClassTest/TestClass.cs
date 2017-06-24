using System;
using System.Diagnostics;
using System.Linq.Expressions;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.StaticMethodInNonStaticClassTest
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
            var adapter = CreateAdapter<IDestinationInterface>(typeof(SourceClass), nameof(SourceClass.Echo));

            adapter.Echo("Input").Should().Be("Input");
        }
    }

    public interface IDestinationInterface
    {
        string Echo(string value);
    }

    public class SourceClass
    {
        public static string Echo(string value)
        {
            return value;
        }
    }
}
