using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.TwoRequestsThatOverrideTwoDifferentOptionalParametersTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TDestination>(object extraParameters, Type @class, string methodName)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter1 = CreateAdapter<IDestinationInterface>(
                new { extraParameter1 = 5},
                typeof(SourceStaticClass),
                nameof(SourceStaticClass.Concat));

            adapter1.Concat("Input").Should().Be("Input52");

            var adapter2 = CreateAdapter<IDestinationInterface>(
                new { extraParameter2 = 5 },
                typeof(SourceStaticClass),
                nameof(SourceStaticClass.Concat));

            adapter2.Concat("Input").Should().Be("Input15");
        }
    }

    public static class SourceStaticClass
    {
        public static string Concat(string value, int extraParameter1 = 1, int extraParameter2 = 2)
        {
            return value + extraParameter1 + extraParameter2;
        }
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }
}
