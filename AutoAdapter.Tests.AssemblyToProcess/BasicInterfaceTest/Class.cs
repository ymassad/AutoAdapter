﻿using System;

namespace AutoAdapter.Tests.AssemblyToProcess.BasicInterfaceTest
{
    public class Class
    {
        [AdapterMethod]
        public TTo CreateAdapter<TFrom, TTo>(TFrom instance)
        {
            throw new Exception();
        }

        public IToInterface Use()
        {
            return CreateAdapter<IFromInterface, IToInterface>(new FromClass());
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
