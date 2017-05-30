using System;
using System.Reflection;

namespace AutoAdapter.Tests
{
    public class Fixture
    {
        private readonly Assembly assmebly;

        private Maybe<string> @namespace;

        private Maybe<string> testClassName;

        private Maybe<string> sourceInterfaceName;

        private Maybe<string> targetInterfaceName;

        private Maybe<string> sourceClassName;

        private Maybe<string> createAdapterMethodName;

        private Maybe<string> nameOfMethodOnTargetInterface;

        private bool testClassIsStatic;

        public Fixture(Assembly assmebly)
        {
            this.assmebly = assmebly;
        }

        public Fixture TestClassIsStatic()
        {
            testClassIsStatic = true;

            return this;
        }

        public Fixture SetNamespace(string @namespace)
        {
            if(this.@namespace.HasValue)
                throw new Exception("namespace is already set");

            this.@namespace = @namespace;

            return this;
        }

        public Fixture SetTestClassName(string name)
        {
            if (this.testClassName.HasValue)
                throw new Exception("Test class name is already set");

            this.testClassName = name;

            return this;
        }

        public Fixture SetSourceInterfaceName(string name)
        {
            if (this.sourceInterfaceName.HasValue)
                throw new Exception("Source interface name is already set");

            this.sourceInterfaceName = name;

            return this;
        }

        public Fixture SetTargetInterfaceName(string name)
        {
            if (this.targetInterfaceName.HasValue)
                throw new Exception("Target interface name is already set");

            this.targetInterfaceName = name;

            return this;
        }

        public Fixture SetSourceClassName(string name)
        {
            if (this.sourceClassName.HasValue)
                throw new Exception("Source class name is already set");

            this.sourceClassName = name;

            return this;
        }

        public Fixture SetCreateAdapterMethodName(string name)
        {
            if (this.createAdapterMethodName.HasValue)
                throw new Exception("CreateAdapter method name is already set");

            this.createAdapterMethodName = name;

            return this;
        }

        public Fixture SetNameOfMethodOnTargetInterface(string name)
        {
            if (this.nameOfMethodOnTargetInterface.HasValue)
                throw new Exception("Name of method on target interface is already set");

            this.nameOfMethodOnTargetInterface = name;

            return this;
        }

        public object InvokeAdapterMethod(params object[] parameters)
        {
            var namePrefix = @namespace.Chain(x => x + ".").GetValueOr("");

            var testClassType = assmebly.GetType($"{namePrefix}{testClassName.GetValueOr("Class")}");

            var createAdapterMethod = testClassType.GetMethod(createAdapterMethodName.GetValueOr("CreateAdapter"));

            var fromInterfaceType = assmebly.GetType($"{namePrefix}{sourceInterfaceName.GetValueOr("IFromInterface")}");

            var fromClassType = assmebly.GetType($"{namePrefix}{sourceClassName.GetValueOr("FromClass")}");

            var fromClassInstance = Activator.CreateInstance(fromClassType);

            var toInterfaceType = assmebly.GetType($"{namePrefix}{targetInterfaceName.GetValueOr("IToInterface")}");

            var closedCreateAdapterMethod = createAdapterMethod.MakeGenericMethod(fromInterfaceType, toInterfaceType);

            object instance = testClassIsStatic ? null : Activator.CreateInstance(testClassType);

            //Act
            var adaptor = closedCreateAdapterMethod.Invoke(instance, new[] { fromClassInstance });

            int extraParameterValue = 0;

            return toInterfaceType.GetMethod(nameOfMethodOnTargetInterface.GetValueOr("Echo")).Invoke(adaptor, parameters);
        }
    }
}