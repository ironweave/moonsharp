using System;
using System.Globalization;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A fixed-width signed 64-bit integer exposed to scripts as the 'int64' userdata type.
	/// Backed by <see cref="System.Int64"/>. Arithmetic is CHECKED: overflow, underflow, and
	/// division by zero raise a <see cref="ScriptRuntimeException"/> instead of silently
	/// wrapping. (Deterministic-trap semantics for consensus code, where a silent wrap is an
	/// exploit primitive, never a feature.)
	///
	/// Operators are provided both between two int64 values and between an int64 and a Lua
	/// integer, so expressions such as <c>int64(10) + 5</c> work directly from script.
	/// Comparison and equality metamethods are dispatched through <see cref="IComparable"/> and
	/// <see cref="object.Equals(object)"/>.
	/// </summary>
	public struct LuaInt64 : IEquatable<LuaInt64>, IComparable<LuaInt64>, IComparable
	{
		/// <summary>
		/// Gets the underlying <see cref="System.Int64"/> value.
		/// </summary>
		public long Value { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LuaInt64"/> struct wrapping the given value.
		/// </summary>
		public LuaInt64(long value)
		{
			Value = value;
		}

		/// <summary>
		/// Parses a decimal string into an int64.
		/// </summary>
		public static LuaInt64 Parse(string s)
		{
			return new LuaInt64(long.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture));
		}

		#region Arithmetic operators (checked/trapping)

		private static LuaInt64 Add(long a, long b)
		{
			try { return new LuaInt64(checked(a + b)); }
			catch (OverflowException) { throw new ScriptRuntimeException("int64 overflow in '+' ({0} + {1})", a, b); }
		}

		private static LuaInt64 Sub(long a, long b)
		{
			try { return new LuaInt64(checked(a - b)); }
			catch (OverflowException) { throw new ScriptRuntimeException("int64 overflow in '-' ({0} - {1})", a, b); }
		}

		private static LuaInt64 Mul(long a, long b)
		{
			try { return new LuaInt64(checked(a * b)); }
			catch (OverflowException) { throw new ScriptRuntimeException("int64 overflow in '*' ({0} * {1})", a, b); }
		}

		private static LuaInt64 Div(long a, long b)
		{
			if (b == 0)
				throw new ScriptRuntimeException("int64 division by zero");
			if (a == long.MinValue && b == -1)
				throw new ScriptRuntimeException("int64 overflow in '/' ({0} / {1})", a, b);
			return new LuaInt64(a / b);
		}

		private static LuaInt64 Mod(long a, long b)
		{
			if (b == 0)
				throw new ScriptRuntimeException("int64 modulo by zero");
			if (a == long.MinValue && b == -1)
				return new LuaInt64(0);
			return new LuaInt64(a % b);
		}

		public static LuaInt64 operator +(LuaInt64 a, LuaInt64 b) { return Add(a.Value, b.Value); }
		public static LuaInt64 operator +(LuaInt64 a, long b) { return Add(a.Value, b); }
		public static LuaInt64 operator +(long a, LuaInt64 b) { return Add(a, b.Value); }

		public static LuaInt64 operator -(LuaInt64 a, LuaInt64 b) { return Sub(a.Value, b.Value); }
		public static LuaInt64 operator -(LuaInt64 a, long b) { return Sub(a.Value, b); }
		public static LuaInt64 operator -(long a, LuaInt64 b) { return Sub(a, b.Value); }

		public static LuaInt64 operator *(LuaInt64 a, LuaInt64 b) { return Mul(a.Value, b.Value); }
		public static LuaInt64 operator *(LuaInt64 a, long b) { return Mul(a.Value, b); }
		public static LuaInt64 operator *(long a, LuaInt64 b) { return Mul(a, b.Value); }

		public static LuaInt64 operator /(LuaInt64 a, LuaInt64 b) { return Div(a.Value, b.Value); }
		public static LuaInt64 operator /(LuaInt64 a, long b) { return Div(a.Value, b); }
		public static LuaInt64 operator /(long a, LuaInt64 b) { return Div(a, b.Value); }

		public static LuaInt64 operator %(LuaInt64 a, LuaInt64 b) { return Mod(a.Value, b.Value); }
		public static LuaInt64 operator %(LuaInt64 a, long b) { return Mod(a.Value, b); }
		public static LuaInt64 operator %(long a, LuaInt64 b) { return Mod(a, b.Value); }

		public static LuaInt64 operator -(LuaInt64 a)
		{
			if (a.Value == long.MinValue)
				throw new ScriptRuntimeException("int64 overflow in unary '-' ({0})", a.Value);
			return new LuaInt64(-a.Value);
		}

		#endregion

		/// <summary>
		/// Returns the absolute value; traps for <c>int64.min</c> (whose absolute value is
		/// not representable).
		/// </summary>
		public LuaInt64 Abs()
		{
			if (Value == long.MinValue)
				throw new ScriptRuntimeException("int64 overflow in 'abs' ({0})", Value);
			return new LuaInt64(Value < 0 ? -Value : Value);
		}

		/// <summary>
		/// Converts to a double-precision Lua number. Magnitudes above 2^53 lose precision.
		/// </summary>
		public double ToNumber()
		{
			return (double)Value;
		}

		[MoonSharpUserDataMetamethod("__tostring")]
		public string ToLuaString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override string ToString()
		{
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override bool Equals(object obj)
		{
			return obj is LuaInt64 && Value == ((LuaInt64)obj).Value;
		}

		public bool Equals(LuaInt64 other)
		{
			return Value == other.Value;
		}

		public int CompareTo(LuaInt64 other)
		{
			return Value.CompareTo(other.Value);
		}

		/// <summary>
		/// Non-generic comparison used by the runtime to dispatch the ordering metamethods.
		/// Supports comparison against another int64 or a Lua number.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is LuaInt64)
				return Value.CompareTo(((LuaInt64)obj).Value);
			if (obj is double || obj is float || obj is decimal)
				return ((double)Value).CompareTo(Convert.ToDouble(obj, CultureInfo.InvariantCulture));
			if (obj is sbyte || obj is byte || obj is short || obj is ushort ||
				obj is int || obj is uint || obj is long)
				return Value.CompareTo(Convert.ToInt64(obj, CultureInfo.InvariantCulture));
			if (obj is ulong)
			{
				ulong u = (ulong)obj;
				return (Value < 0) ? -1 : ((ulong)Value).CompareTo(u);
			}

			throw new ArgumentException("Cannot compare an int64 with " + (obj == null ? "nil" : obj.GetType().Name));
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}
