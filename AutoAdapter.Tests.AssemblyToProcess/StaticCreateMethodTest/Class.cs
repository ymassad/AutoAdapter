using System;

namespace AutoAdapter.Tests.AssemblyToProcess.StaticCreateMethodTest
{
    public static class Class
    {
        [AdapterMethod]
        public static TTo CreateAdapter<TFrom, TTo>(TFrom instance)
        {
            throw new Exception();
        }

        public static IToInterface Use()
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
