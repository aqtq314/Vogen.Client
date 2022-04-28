using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vogen.Client.ViewModels.Utils
{
    public struct NonEquatable<T> : IEquatable<NonEquatable<T>>
    {
        public override bool Equals([NotNullWhen(true)] object? obj) => false;
        public override int GetHashCode() => 0x55555555;
        public bool Equals(NonEquatable<T> other) => false;

        public static bool operator ==(NonEquatable<T> left, NonEquatable<T> right) => false;
        public static bool operator !=(NonEquatable<T> left, NonEquatable<T> right) => true;

        public T Value { get; set; }

        public NonEquatable(T value)
        {
            Value = value;
        }
    }
}
