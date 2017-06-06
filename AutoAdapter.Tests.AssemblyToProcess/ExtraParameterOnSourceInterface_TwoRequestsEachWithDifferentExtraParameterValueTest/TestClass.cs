using FluentAssertions;
using System;

namespace AutoAdapter.Tests.AssemblyToProcess.ExtraParameterOnSourceInterface_TwoRequestsEachWithDifferentExtraParameterValueTest
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
                new { extraParameter = 1 });

            adapter1.Concat("Input").Should().Be("Input1");

            var adapter2 = CreateAdapter<ISourceInterface, IDestinationInterface>(
                new SourceClass(),
                new { extraParameter = 2 });

            adapter2.Concat("Input").Should().Be("Input2");
        }
    }

    public interface ISourceInterface
    {
        string Concat(string value, int extraParameter);
    }

    public interface IDestinationInterface
    {
        string Concat(string value);
    }

    public class SourceClass : ISourceInterface
    {
        public string Concat(string value, int extraParameter)
        {
            return value + extraParameter;
        }
    }
}
