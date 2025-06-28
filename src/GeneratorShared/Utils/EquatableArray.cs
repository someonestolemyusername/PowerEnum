using System;

namespace PowerEnum.SourceGenerator.Models
{
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
        where T : IEquatable<T>
    {
        private readonly T[] _array;
        private readonly int _length;

        public EquatableArray()
        {
            _array = [];
            _length = 0;
        }

        public EquatableArray(T[] array)
        {
            _array = array;
            _length = array.Length;
        }

        public EquatableArray(T[] array, int length)
        {
            if (length > array.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "Length cannot be greater than the array length.");
            }

            _array = array;
            _length = length;
        }

        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return _array.AsSpan(0, _length);
        }

        public int Length => _length;

        public bool Equals(EquatableArray<T> other)
        {
            return AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());
        }

        public override bool Equals(object obj)
        {
            return obj is EquatableArray<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = 1;

                foreach (ref readonly var item in AsReadOnlySpan())
                {
                    result = 31 * result + (item?.GetHashCode() ?? 0);
                }

                return result;
            }
        }

        public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
        {
            return !left.Equals(right);
        }
    }
}
