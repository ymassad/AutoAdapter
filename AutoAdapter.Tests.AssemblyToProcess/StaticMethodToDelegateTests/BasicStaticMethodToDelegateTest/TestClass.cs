using System;
using System.Diagnostics;
using System.Linq.Expressions;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToDelegateTests.BasicStaticMethodToDelegateTest
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
            var adapter = CreateAdapter<Echo>(typeof(SourceStaticClass), nameof(SourceStaticClass.Echo));

            adapter("Input").Should().Be("Input");
        }
    }

    public delegate string Echo(string value);

    public static class SourceStaticClass
    {
        public static string Echo(string value)
        {
            return value;
        }
    }
}
