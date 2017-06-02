using FluentAssertions;
using System;

namespace AutoAdapter.Tests.AssemblyToProcess.ExtraParameterOnSourceInterfaceTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TTo CreateAdapter<TFrom, TTo>(TFrom instance, object extraParameters)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<IFromInterface, IToInterface>(
                new FromClass(),
                new {extraParameter = 1});

            adapter.Concat("Input").Should().Be("Input1");
        }
    }

    public interface IFromInterface
    {
        string Concat(string value, int extraParameter);
    }

    public interface IToInterface
    {
        string Concat(string value);
    }

    public class FromClass : IFromInterface
    {
        public string Concat(string value, int extraParameter)
        {
            return value + extraParameter;
        }
    }
}
