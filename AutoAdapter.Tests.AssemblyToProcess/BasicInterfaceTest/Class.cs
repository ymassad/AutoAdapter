using System;

namespace AutoAdapter.Tests.AssemblyToProcess.BasicInterfaceTest
{
    public class Class
    {
        [AdapterMethod]
        public TTo CreateAdapter<TFrom, TTo>(TFrom instance)
        {
            throw new Exception();
        }

        public void Consumer()
        {
            CreateAdapter<IFromInterface, IToInterface>(null);
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
