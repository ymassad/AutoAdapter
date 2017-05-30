using System;
using System.Reflection;

namespace AutoAdapter.Tests
{
    public class Fixture
    {
        private readonly Assembly assmebly;

        private Maybe<string> @namespace;

        private Maybe<string> testClassName;

        private Maybe<string> useMethodName;

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

        public Fixture SetUseMethodName(string name)
        {
            if (useMethodName.HasValue)
                throw new Exception("'Use' method name is already set");

            useMethodName = name;

            return this;
        }


        public Fixture SetNamespace(string name)
        {
            if(@namespace.HasValue)
                throw new Exception("namespace is already set");

            @namespace = name;

            return this;
        }

        public Fixture SetTestClassName(string name)
        {
            if (testClassName.HasValue)
                throw new Exception("Test class name is already set");

            testClassName = name;

            return this;
        }

        public Fixture SetNameOfMethodOnTargetInterface(string name)
        {
            if (nameOfMethodOnTargetInterface.HasValue)
                throw new Exception("Name of method on target interface is already set");

            nameOfMethodOnTargetInterface = name;

            return this;
        }

        public object InvokeAdapterMethod(params object[] parameters)
        {
            var namePrefix = @namespace.Chain(x => x + ".").GetValueOr("");

            var testClassType = assmebly.GetType($"{namePrefix}{testClassName.GetValueOr("Class")}");

            object instance = testClassIsStatic ? null : Activator.CreateInstance(testClassType);

            var useMethod = testClassType.GetMethod(useMethodName.GetValueOr("Use"));

            //Act
            var adaptor = useMethod.Invoke(instance, new object[]{});

            return adaptor.GetType().GetMethod(nameOfMethodOnTargetInterface.GetValueOr("Echo")).Invoke(adaptor, parameters);
        }
    }
}