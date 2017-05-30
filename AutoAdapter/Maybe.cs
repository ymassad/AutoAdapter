using System;

namespace AutoAdapter
{
    public struct Maybe<T>
    {
        private readonly bool hasValue;

        private readonly T value;

        private Maybe(T value)
        {
            hasValue = true;
            this.value = value;
        }

        public bool HasValue => hasValue;

        public bool HasNoValue => !hasValue;

        public T GetValue()
        {
            if(!hasValue)
                throw new Exception("There is no value");

            return value;
        }

        public T GetValueOr(T defaultValue)
        {
            if (hasValue)
                return value;

            return defaultValue;
        }

        public T GetValueOr(Func<T> defaultValueFactory)
        {
            if (hasValue)
                return value;

            return defaultValueFactory();
        }

        public static Maybe<T> OfValue(T value)
        {
            if(value == null)
                throw new Exception("value should not be null");

            return new Maybe<T>(value);
        }

        public static Maybe<T> NoValue()
        {
            return new Maybe<T>();
        }

        public static implicit operator Maybe<T>(T value)
        {
            if (value == null)
                return NoValue();

            return OfValue(value);
        }

        public Maybe<R> Chain<R>(Func<T, R> function)
        {
            if (!hasValue)
                return new Maybe<R>();

            return new Maybe<R>(function(value));
        }
    }
}
