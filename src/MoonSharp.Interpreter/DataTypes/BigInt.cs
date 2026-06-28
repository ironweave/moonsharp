using System;
using System.Globalization;
using System.Numerics;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// An arbitrary-precision integer exposed to scripts as the 'bigint' userdata type.
	/// Backed by <see cref="System.Numerics.BigInteger"/>.
	///
	/// Instances are immutable. Arithmetic and comparison operators are provided both
	/// between two bigints and between a bigint and a (CLR <c>long</c>) Lua integer, so
	/// expressions such as <c>bigint(10) + 5</c> work directly from script.
	/// </summary>
	public struct BigInt : IEquatable<BigInt>, IComparable<BigInt>, IComparable
	{
		/// <summary>
		/// Gets the underlying <see cref="System.Numerics.BigInteger"/> value.
		/// </summary>
		public BigInteger Value { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BigInt"/> struct wrapping the given value.
		/// </summary>
		public BigInt(BigInteger value)
		{
			Value = value;
		}

		/// <summary>
		/// Parses a decimal string into a bigint.
		/// </summary>
		public static BigInt Parse(string s)
		{
			return new BigInt(BigInteger.Parse(s, CultureInfo.InvariantCulture));
		}

		#region Arithmetic operators

		public static BigInt operator +(BigInt a, BigInt b) { return new BigInt(a.Value + b.Value); }
		public static BigInt operator +(BigInt a, long b) { return new BigInt(a.Value + b); }
		public static BigInt operator +(long a, BigInt b) { return new BigInt(a + b.Value); }

		public static BigInt operator -(BigInt a, BigInt b) { return new BigInt(a.Value - b.Value); }
		public static BigInt operator -(BigInt a, long b) { return new BigInt(a.Value - b); }
		public static BigInt operator -(long a, BigInt b) { return new BigInt(a - b.Value); }

		public static BigInt operator *(BigInt a, BigInt b) { return new BigInt(a.Value * b.Value); }
		public static BigInt operator *(BigInt a, long b) { return new BigInt(a.Value * b); }
		public static BigInt operator *(long a, BigInt b) { return new BigInt(a * b.Value); }

		public static BigInt operator /(BigInt a, BigInt b) { return new BigInt(a.Value / b.Value); }
		public static BigInt operator /(BigInt a, long b) { return new BigInt(a.Value / b); }
		public static BigInt operator /(long a, BigInt b) { return new BigInt(a / b.Value); }

		public static BigInt operator %(BigInt a, BigInt b) { return new BigInt(a.Value % b.Value); }
		public static BigInt operator %(BigInt a, long b) { return new BigInt(a.Value % b); }
		public static BigInt operator %(long a, BigInt b) { return new BigInt(a % b.Value); }

		public static BigInt operator -(BigInt a) { return new BigInt(-a.Value); }

		#endregion

		#region Comparison operators

		public static bool operator ==(BigInt a, BigInt b) { return a.Value == b.Value; }
		public static bool operator !=(BigInt a, BigInt b) { return a.Value != b.Value; }
		public static bool operator <(BigInt a, BigInt b) { return a.Value < b.Value; }
		public static bool operator <=(BigInt a, BigInt b) { return a.Value <= b.Value; }
		public static bool operator >(BigInt a, BigInt b) { return a.Value > b.Value; }
		public static bool operator >=(BigInt a, BigInt b) { return a.Value >= b.Value; }

		public static bool operator ==(BigInt a, long b) { return a.Value == b; }
		public static bool operator ==(long a, BigInt b) { return a == b.Value; }
		public static bool operator !=(BigInt a, long b) { return a.Value != b; }
		public static bool operator !=(long a, BigInt b) { return a != b.Value; }
		public static bool operator <(BigInt a, long b) { return a.Value < b; }
		public static bool operator <(long a, BigInt b) { return a < b.Value; }
		public static bool operator <=(BigInt a, long b) { return a.Value <= b; }
		public static bool operator <=(long a, BigInt b) { return a <= b.Value; }
		public static bool operator >(BigInt a, long b) { return a.Value > b; }
		public static bool operator >(long a, BigInt b) { return a > b.Value; }
		public static bool operator >=(BigInt a, long b) { return a.Value >= b; }
		public static bool operator >=(long a, BigInt b) { return a >= b.Value; }

		#endregion

		/// <summary>
		/// Raises this value to the given (non-negative) power.
		/// </summary>
		public BigInt Pow(int exponent)
		{
			return new BigInt(BigInteger.Pow(Value, exponent));
		}

		/// <summary>
		/// Returns the absolute value.
		/// </summary>
		public BigInt Abs()
		{
			return new BigInt(BigInteger.Abs(Value));
		}

		/// <summary>
		/// Converts to a double-precision Lua number. Large magnitudes lose precision.
		/// </summary>
		public double ToNumber()
		{
			return (double)Value;
		}

		/// <summary>
		/// Metamethod handler for 'tostring' / string coercion. Returns the decimal representation.
		/// </summary>
		[MoonSharpUserDataMetamethod("__tostring")]
		public string ToLuaString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Returns the decimal string representation of the value.
		/// </summary>
		public override string ToString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override bool Equals(object obj)
		{
			return obj is BigInt && Value == ((BigInt)obj).Value;
		}

		public bool Equals(BigInt other)
		{
			return Value == other.Value;
		}

		public int CompareTo(BigInt other)
		{
			return Value.CompareTo(other.Value);
		}

		/// <summary>
		/// Non-generic comparison, used by the runtime to dispatch the '&lt;', '&lt;=', '&gt;' and '&gt;='
		/// metamethods. Supports comparison against another bigint or a Lua number.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is BigInt)
				return Value.CompareTo(((BigInt)obj).Value);
			if (obj is BigInteger)
				return Value.CompareTo((BigInteger)obj);
			if (obj is double || obj is float || obj is decimal)
				return ((double)Value).CompareTo(Convert.ToDouble(obj, CultureInfo.InvariantCulture));
			if (obj is sbyte || obj is byte || obj is short || obj is ushort ||
				obj is int || obj is uint || obj is long)
				return Value.CompareTo(new BigInteger(Convert.ToInt64(obj, CultureInfo.InvariantCulture)));
			if (obj is ulong)
				return Value.CompareTo(new BigInteger(Convert.ToUInt64(obj, CultureInfo.InvariantCulture)));

			throw new ArgumentException("Cannot compare a bigint with " + (obj == null ? "nil" : obj.GetType().Name));
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}
