﻿using System;
using FluentAssertions;

namespace AutoAdapter.Tests.AssemblyToProcess.BasicInterfaceTest
{
    public class TestClass
    {
        [AdapterMethod]
        public TTo CreateAdapter<TFrom, TTo>(TFrom instance)
        {
            throw new Exception();
        }

        public void RunTest()
        {
            var adapter = CreateAdapter<IFromInterface, IToInterface>(new FromClass());

            adapter.Echo("Input").Should().Be("Input");
        }
    }

    public interface IFromInterface
    {
        string Echo(string value);
    }

    public interface IToInterface
    {
        string Echo(string value);
    }

    public class FromClass : IFromInterface
    {
        public string Echo(string value)
        {
            return value;
        }
    }
}