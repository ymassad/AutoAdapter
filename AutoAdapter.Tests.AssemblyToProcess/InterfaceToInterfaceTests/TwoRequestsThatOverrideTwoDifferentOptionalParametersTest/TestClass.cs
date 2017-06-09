using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.InterfaceToInterfaceTests.TwoRequestsThatOverrideTwoDifferentOptionalParametersTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TSource, TDestination>(TSource instance, object extraParameters)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter1 = CreateAdapter<ISourceInterface, IDestinationInterface>(
                new SourceClass(),
                new { extraParameter1 = 5});

            adapter1.Concat("Input").Should().Be("Input52");

            var adapter2 = CreateAdapter<ISourceInterface, IDestinationInterface>(
                new SourceClass(),
                new { extraParameter2 = 5 });

            adapter2.Concat("Input").Should().Be("Input15");
        }
    }

    public interface ISourceInterface
    {
        string Concat(string value, int extraParameter1 = 1, int extraParameter2 = 2);
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Concat(string value, int extraParameter1, int extraParameter2)
        {
            return value + extraParameter1 + extraParameter2;
        }
    }
}
