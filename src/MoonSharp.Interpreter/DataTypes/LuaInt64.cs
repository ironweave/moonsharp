using System;
using System.Globalization;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A fixed-width signed 64-bit integer exposed to scripts as the 'int64' userdata type.
	/// Backed by <see cref="System.Int64"/>. Arithmetic wraps around (unchecked), matching the
	/// default behaviour of .NET <c>long</c> operators.
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

		#region Arithmetic operators (wrapping)

		public static LuaInt64 operator +(LuaInt64 a, LuaInt64 b) { return new LuaInt64(unchecked(a.Value + b.Value)); }
		public static LuaInt64 operator +(LuaInt64 a, long b) { return new LuaInt64(unchecked(a.Value + b)); }
		public static LuaInt64 operator +(long a, LuaInt64 b) { return new LuaInt64(unchecked(a + b.Value)); }

		public static LuaInt64 operator -(LuaInt64 a, LuaInt64 b) { return new LuaInt64(unchecked(a.Value - b.Value)); }
		public static LuaInt64 operator -(LuaInt64 a, long b) { return new LuaInt64(unchecked(a.Value - b)); }
		public static LuaInt64 operator -(long a, LuaInt64 b) { return new LuaInt64(unchecked(a - b.Value)); }

		public static LuaInt64 operator *(LuaInt64 a, LuaInt64 b) { return new LuaInt64(unchecked(a.Value * b.Value)); }
		public static LuaInt64 operator *(LuaInt64 a, long b) { return new LuaInt64(unchecked(a.Value * b)); }
		public static LuaInt64 operator *(long a, LuaInt64 b) { return new LuaInt64(unchecked(a * b.Value)); }

		public static LuaInt64 operator /(LuaInt64 a, LuaInt64 b) { return new LuaInt64(a.Value / b.Value); }
		public static LuaInt64 operator /(LuaInt64 a, long b) { return new LuaInt64(a.Value / b); }
		public static LuaInt64 operator /(long a, LuaInt64 b) { return new LuaInt64(a / b.Value); }

		public static LuaInt64 operator %(LuaInt64 a, LuaInt64 b) { return new LuaInt64(a.Value % b.Value); }
		public static LuaInt64 operator %(LuaInt64 a, long b) { return new LuaInt64(a.Value % b); }
		public static LuaInt64 operator %(long a, LuaInt64 b) { return new LuaInt64(a % b.Value); }

		public static LuaInt64 operator -(LuaInt64 a) { return new LuaInt64(unchecked(-a.Value)); }

		#endregion

		/// <summary>
		/// Returns the absolute value (wraps for <c>int64.min</c>).
		/// </summary>
		public LuaInt64 Abs()
		{
			return new LuaInt64(unchecked(Value < 0 ? -Value : Value));
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
