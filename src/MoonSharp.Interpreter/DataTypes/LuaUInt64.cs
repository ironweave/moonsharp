using System;
using System.Globalization;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A fixed-width unsigned 64-bit integer exposed to scripts as the 'uint64' userdata type.
	/// Backed by <see cref="System.UInt64"/>. Arithmetic wraps around (unchecked), matching the
	/// default behaviour of .NET <c>ulong</c> operators.
	///
	/// Operators are provided both between two uint64 values and between a uint64 and a Lua
	/// integer (which is reinterpreted as unsigned), so expressions such as <c>uint64(10) + 5</c>
	/// work directly from script. Comparison and equality metamethods are dispatched through
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

		#region Arithmetic operators (wrapping)

		public static LuaUInt64 operator +(LuaUInt64 a, LuaUInt64 b) { return new LuaUInt64(unchecked(a.Value + b.Value)); }
		public static LuaUInt64 operator +(LuaUInt64 a, long b) { return new LuaUInt64(unchecked(a.Value + (ulong)b)); }
		public static LuaUInt64 operator +(long a, LuaUInt64 b) { return new LuaUInt64(unchecked((ulong)a + b.Value)); }

		public static LuaUInt64 operator -(LuaUInt64 a, LuaUInt64 b) { return new LuaUInt64(unchecked(a.Value - b.Value)); }
		public static LuaUInt64 operator -(LuaUInt64 a, long b) { return new LuaUInt64(unchecked(a.Value - (ulong)b)); }
		public static LuaUInt64 operator -(long a, LuaUInt64 b) { return new LuaUInt64(unchecked((ulong)a - b.Value)); }

		public static LuaUInt64 operator *(LuaUInt64 a, LuaUInt64 b) { return new LuaUInt64(unchecked(a.Value * b.Value)); }
		public static LuaUInt64 operator *(LuaUInt64 a, long b) { return new LuaUInt64(unchecked(a.Value * (ulong)b)); }
		public static LuaUInt64 operator *(long a, LuaUInt64 b) { return new LuaUInt64(unchecked((ulong)a * b.Value)); }

		public static LuaUInt64 operator /(LuaUInt64 a, LuaUInt64 b) { return new LuaUInt64(a.Value / b.Value); }
		public static LuaUInt64 operator /(LuaUInt64 a, long b) { return new LuaUInt64(a.Value / (ulong)b); }
		public static LuaUInt64 operator /(long a, LuaUInt64 b) { return new LuaUInt64((ulong)a / b.Value); }

		public static LuaUInt64 operator %(LuaUInt64 a, LuaUInt64 b) { return new LuaUInt64(a.Value % b.Value); }
		public static LuaUInt64 operator %(LuaUInt64 a, long b) { return new LuaUInt64(a.Value % (ulong)b); }
		public static LuaUInt64 operator %(long a, LuaUInt64 b) { return new LuaUInt64((ulong)a % b.Value); }

		// Two's-complement negation (wraps), since ulong has no native unary minus.
		public static LuaUInt64 operator -(LuaUInt64 a) { return new LuaUInt64(unchecked(0UL - a.Value)); }

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
			return obj is LuaUInt64 && Value == ((LuaUInt64)obj).Value;
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
		/// Supports comparison against another uint64 or a Lua number.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is LuaUInt64)
				return Value.CompareTo(((LuaUInt64)obj).Value);
			if (obj is double || obj is float || obj is decimal)
				return ((double)Value).CompareTo(Convert.ToDouble(obj, CultureInfo.InvariantCulture));
			if (obj is ulong)
				return Value.CompareTo((ulong)obj);
			if (obj is byte || obj is ushort || obj is uint)
				return Value.CompareTo(Convert.ToUInt64(obj, CultureInfo.InvariantCulture));
			if (obj is sbyte || obj is short || obj is int || obj is long)
			{
				long l = Convert.ToInt64(obj, CultureInfo.InvariantCulture);
				return (l < 0) ? 1 : Value.CompareTo((ulong)l);
			}

			throw new ArgumentException("Cannot compare a uint64 with " + (obj == null ? "nil" : obj.GetType().Name));
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}
