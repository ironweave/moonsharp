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
	/// expressions such as <c>bigint(10) + 5</c> work directly from script. BigInteger cannot
	/// overflow, but division and modulo by zero trap as a <see cref="ScriptRuntimeException"/>
	/// rather than escaping as a raw CLR exception (matching int64/uint64/decimal).
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
		/// Parses a decimal string into a bigint. Raises a script error (never a raw CLR
		/// FormatException, which Lua's pcall cannot catch) on unparseable input — this method
		/// is script-reachable as a static member of the registered userdata type.
		/// </summary>
		public static BigInt Parse(string s)
		{
			BigInteger parsed;
			if (!BigInteger.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("cannot parse '{0}' as a bigint", s);
			return new BigInt(parsed);
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

		// Division and modulo trap on a zero divisor (matching int64/uint64/decimal) rather
		// than letting a raw DivideByZeroException escape the interpreter. BigInteger cannot
		// overflow, so these are the only trapping cases.
		private static BigInt Div(BigInteger a, BigInteger b)
		{
			if (b.IsZero)
				throw new ScriptRuntimeException("bigint division by zero");
			return new BigInt(a / b);
		}

		private static BigInt Mod(BigInteger a, BigInteger b)
		{
			if (b.IsZero)
				throw new ScriptRuntimeException("bigint modulo by zero");
			return new BigInt(a % b);
		}

		public static BigInt operator /(BigInt a, BigInt b) { return Div(a.Value, b.Value); }
		public static BigInt operator /(BigInt a, long b) { return Div(a.Value, b); }
		public static BigInt operator /(long a, BigInt b) { return Div(a, b.Value); }

		public static BigInt operator %(BigInt a, BigInt b) { return Mod(a.Value, b.Value); }
		public static BigInt operator %(BigInt a, long b) { return Mod(a.Value, b); }
		public static BigInt operator %(long a, BigInt b) { return Mod(a, b.Value); }

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
			if (obj is BigInt) return Value == ((BigInt)obj).Value;
			// Value-equal to any other numeric type (bigint(5) == 5 == int64(5) == decimal(5)).
			return NumericInterop.AreEqual(Value, obj);
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
		/// metamethods. Supports comparison against another bigint, any sibling numeric type, or a Lua number.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is BigInt)
				return Value.CompareTo(((BigInt)obj).Value);
			return NumericInterop.Compare(Value, obj);
		}

		public override int GetHashCode()
		{
			// Value-based so it stays consistent with cross-type equality (bigint(5) == 5 == decimal(5)).
			return NumericInterop.ValueHashCode(Value);
		}
	}
}
