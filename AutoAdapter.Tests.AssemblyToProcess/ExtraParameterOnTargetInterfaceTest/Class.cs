using System;

namespace AutoAdapter.Tests.AssemblyToProcess.ExtraParameterOnTargetInterfaceTest
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
