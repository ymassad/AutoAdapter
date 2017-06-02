using FluentAssertions;
using System;

namespace AutoAdapter.Tests.AssemblyToProcess.ExtraParameterOnTargetInterfaceTest
{
    public class TestClass
    {
        //[AdapterMethod]
        public TTo CreateAdapter<TFrom, TTo>(TFrom instance)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<IFromInterface, IToInterface>(new FromClass());

            var extraParameter = 0;

            adapter.Echo("Input", extraParameter).Should().Be("Input");
        }
    }

    public interface IFromInterface
    {
        string Echo(string value);
    }

    public interface IToInterface
    {
        string Echo(string value, int extraParameter);
    }

    public class FromClass : IFromInterface
    {
        public string Echo(string value)
        {
            return value;
        }
    }
}
