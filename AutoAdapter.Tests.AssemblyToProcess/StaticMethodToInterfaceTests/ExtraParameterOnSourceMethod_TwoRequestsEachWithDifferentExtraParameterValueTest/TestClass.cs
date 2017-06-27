using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticMethodToInterfaceTests.ExtraParameterOnSourceMethod_TwoRequestsEachWithDifferentExtraParameterValueTest
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
            var adapter = CreateAdapter<IDestinationInterface>(
                new { extraParameter = 1 },
                typeof(SourceStaticClass),
                nameof(SourceStaticClass.Concat));

            adapter.Concat("Input").Should().Be("Input1");

            var adapter2 = CreateAdapter<IDestinationInterface>(
                new { extraParameter = 2 },
                typeof(SourceStaticClass),
                nameof(SourceStaticClass.Concat));

            adapter2.Concat("Input").Should().Be("Input2");
        }
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public static class SourceStaticClass
    {
        public static string Concat(string value, int extraParameter)
        {
            return value + extraParameter;
        }
    }
}
