﻿using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.OptionalExtraParameterOnSourceInterfaceWithConstantDefaultValueTest.Int32Constant
{
    public class TestClass
    {
        //[AdapterMethod]
        public TDestination CreateAdapter<TSource, TDestination>(TSource instance)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<ISourceInterface, IDestinationInterface>(
                new SourceClass());

            adapter.Concat("Input").Should().Be("Input1");
        }
    }

    public interface ISourceInterface
    {
        string Concat(string value, int extraParameter = 1);
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
