using System;
using System.Globalization;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A fixed-width unsigned 64-bit integer exposed to scripts as the 'uint64' userdata type.
	/// Backed by <see cref="System.UInt64"/>. Arithmetic is CHECKED: overflow, underflow,
	/// division by zero, and negative Lua-number operands raise a
	/// <see cref="ScriptRuntimeException"/> instead of silently wrapping. (Deterministic-trap
	/// semantics: these values carry token amounts in consensus code, where a silent wrap is
	/// an exploit primitive, never a feature.)
	///
	/// Operators are provided both between two uint64 values and between a uint64 and a Lua
	/// integer, so expressions such as <c>uint64(10) + 5</c> work directly from script.
	/// Comparison and equality metamethods are dispatched through
	/// <see cref="IComparable"/> and <see cref="object.Equals(object)"/>.
	/// </summary>
	public struct LuaUInt64 : IEquatable<LuaUInt64>, IComparable<LuaUInt64>, IComparable
	{
		/// <summary>
		/// Gets the underlying <see cref="System.UInt64"/> value.
		/// </summary>
		public ulong Value { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LuaUInt64"/> struct wrapping the given value.
		/// </summary>
		public LuaUInt64(ulong value)
		{
			Value = value;
		}

		/// <summary>
		/// Parses a decimal string into a uint64.
		/// </summary>
		public static LuaUInt64 Parse(string s)
		{
			return new LuaUInt64(ulong.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture));
		}

		#region Arithmetic operators (checked/trapping)

		/// <summary>
		/// Converts a Lua-number operand to unsigned, trapping on negative values instead of
		/// reinterpreting the bit pattern.
		/// </summary>
		private static ulong Operand(long b, string op)
		{
			if (b < 0)
				throw new ScriptRuntimeException("uint64 arithmetic error in '{0}': negative operand ({1})", op, b);
			return (ulong)b;
		}

		private static LuaUInt64 Add(ulong a, ulong b)
		{
			ulong r = unchecked(a + b);
			if (r < a)
				throw new ScriptRuntimeException("uint64 overflow in '+' ({0} + {1})", a, b);
			return new LuaUInt64(r);
		}

		private static LuaUInt64 Sub(ulong a, ulong b)
		{
			if (b > a)
				throw new ScriptRuntimeException("uint64 underflow in '-' ({0} - {1})", a, b);
			return new LuaUInt64(a - b);
		}

		private static LuaUInt64 Mul(ulong a, ulong b)
		{
			ulong r = unchecked(a * b);
			if (a != 0 && r / a != b)
				throw new ScriptRuntimeException("uint64 overflow in '*' ({0} * {1})", a, b);
			return new LuaUInt64(r);
		}

		private static LuaUInt64 Div(ulong a, ulong b)
		{
			if (b == 0)
				throw new ScriptRuntimeException("uint64 division by zero");
			return new LuaUInt64(a / b);
		}

		private static LuaUInt64 Mod(ulong a, ulong b)
		{
			if (b == 0)
				throw new ScriptRuntimeException("uint64 modulo by zero");
			return new LuaUInt64(a % b);
		}

		public static LuaUInt64 operator +(LuaUInt64 a, LuaUInt64 b) { return Add(a.Value, b.Value); }
		public static LuaUInt64 operator +(LuaUInt64 a, long b) { return Add(a.Value, Operand(b, "+")); }
		public static LuaUInt64 operator +(long a, LuaUInt64 b) { return Add(Operand(a, "+"), b.Value); }

		public static LuaUInt64 operator -(LuaUInt64 a, LuaUInt64 b) { return Sub(a.Value, b.Value); }
		public static LuaUInt64 operator -(LuaUInt64 a, long b) { return Sub(a.Value, Operand(b, "-")); }
		public static LuaUInt64 operator -(long a, LuaUInt64 b) { return Sub(Operand(a, "-"), b.Value); }

		public static LuaUInt64 operator *(LuaUInt64 a, LuaUInt64 b) { return Mul(a.Value, b.Value); }
		public static LuaUInt64 operator *(LuaUInt64 a, long b) { return Mul(a.Value, Operand(b, "*")); }
		public static LuaUInt64 operator *(long a, LuaUInt64 b) { return Mul(Operand(a, "*"), b.Value); }

		public static LuaUInt64 operator /(LuaUInt64 a, LuaUInt64 b) { return Div(a.Value, b.Value); }
		public static LuaUInt64 operator /(LuaUInt64 a, long b) { return Div(a.Value, Operand(b, "/")); }
		public static LuaUInt64 operator /(long a, LuaUInt64 b) { return Div(Operand(a, "/"), b.Value); }

		public static LuaUInt64 operator %(LuaUInt64 a, LuaUInt64 b) { return Mod(a.Value, b.Value); }
		public static LuaUInt64 operator %(LuaUInt64 a, long b) { return Mod(a.Value, Operand(b, "%")); }
		public static LuaUInt64 operator %(long a, LuaUInt64 b) { return Mod(Operand(a, "%"), b.Value); }

		// Unary minus on an unsigned value is meaningful only for zero; anything else traps.
		public static LuaUInt64 operator -(LuaUInt64 a)
		{
			if (a.Value != 0)
				throw new ScriptRuntimeException("uint64 underflow in unary '-' ({0})", a.Value);
			return new LuaUInt64(0);
		}

		#endregion

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
			if (obj is LuaUInt64) return Value == ((LuaUInt64)obj).Value;
			// Value-equal to any other numeric type (uint64(5) == 5 == int64(5) == decimal(5)).
			return NumericInterop.AreEqual(Value, obj);
		}

		public bool Equals(LuaUInt64 other)
		{
			return Value == other.Value;
		}

		public int CompareTo(LuaUInt64 other)
		{
			return Value.CompareTo(other.Value);
		}

		/// <summary>
		/// Non-generic comparison used by the runtime to dispatch the ordering metamethods.
		/// Supports comparison against another uint64, any sibling numeric type, or a Lua number.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is LuaUInt64)
				return Value.CompareTo(((LuaUInt64)obj).Value);
			return NumericInterop.Compare(Value, obj);
		}

		public override int GetHashCode()
		{
			// Value-based so it stays consistent with cross-type equality (uint64(5) == 5 == decimal(5)).
			return NumericInterop.ValueHashCode(Value);
		}
	}
}
