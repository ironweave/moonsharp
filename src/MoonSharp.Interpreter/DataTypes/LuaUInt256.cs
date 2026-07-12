using System;
using System.Globalization;
using System.Numerics;

namespace MoonSharp.Interpreter
{
	/// <summary>
	/// A fixed-width unsigned 256-bit integer exposed to scripts as the 'uint256' userdata type.
	/// Backed by <see cref="System.Numerics.BigInteger"/> with a hard width invariant: every value
	/// satisfies <c>0 &lt;= Value &lt; 2^256</c>. Arithmetic is CHECKED: overflow past 2^256,
	/// underflow below zero, division/modulo by zero, and negative Lua-number operands raise a
	/// <see cref="ScriptRuntimeException"/> instead of silently wrapping. (Deterministic-trap
	/// semantics: these values carry wide intermediate quantities in consensus code — constant-
	/// product AMM invariants, share bootstraps — where a silent wrap or truncation is an exploit
	/// primitive, never a feature.)
	///
	/// The BigInteger backing reuses .NET's battle-tested exact-integer multiply/divide/modulo and
	/// comparison rather than hand-rolled limb arithmetic; the type's only bespoke logic is the
	/// range guard applied after each operation and an integer square root. Both are exercised by
	/// differential tests. BigInteger arithmetic is deterministic across platforms, so identical
	/// inputs yield identical results on every validator.
	///
	/// Operators are provided both between two uint256 values and between a uint256 and a Lua
	/// integer, so expressions such as <c>uint256(10) + 5</c> work directly from script. Value
	/// equality and ordering interoperate with the sibling numeric types through
	/// <see cref="NumericInterop"/>, so <c>uint256(5) == 5 == uint64(5) == bigint(5)</c>.
	/// </summary>
	public struct LuaUInt256 : IEquatable<LuaUInt256>, IComparable<LuaUInt256>, IComparable
	{
		/// <summary>2^256 — the exclusive upper bound of the representable range.</summary>
		public static readonly BigInteger Modulus = BigInteger.One << 256;

		/// <summary>The largest representable value, 2^256 - 1.</summary>
		public static readonly BigInteger MaxValue = Modulus - BigInteger.One;

		/// <summary>
		/// Gets the underlying value. Invariant: <c>0 &lt;= Value &lt; 2^256</c>.
		/// </summary>
		public BigInteger Value { get; private set; }

		/// <summary>
		/// Initializes a new instance wrapping the given value, trapping if it is negative or
		/// does not fit in 256 bits (so a LuaUInt256 can never hold an out-of-range value).
		/// </summary>
		public LuaUInt256(BigInteger value)
		{
			if (value.Sign < 0)
				throw new ScriptRuntimeException("uint256 underflow (negative value {0})", value);
			if (value >= Modulus)
				throw new ScriptRuntimeException("uint256 overflow (value does not fit in 256 bits)");
			Value = value;
		}

		/// <summary>
		/// Parses a decimal string into a uint256. Raises a script error (never a raw CLR
		/// FormatException/OverflowException, which Lua's pcall cannot catch) on unparseable or
		/// out-of-range input — this method is script-reachable as a static member of the type.
		/// </summary>
		public static LuaUInt256 Parse(string s)
		{
			BigInteger parsed;
			if (!BigInteger.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("cannot parse '{0}' as a uint256", s);
			if (parsed.Sign < 0 || parsed >= Modulus)
				throw new ScriptRuntimeException("value '{0}' is out of range for uint256", s);
			return new LuaUInt256(parsed);
		}

		/// <summary>
		/// Parses a hex string (with or without a leading "0x", up to 64 hex digits) into a
		/// uint256. Script-reachable; raises a script error on non-hex or over-length input.
		/// </summary>
		public static LuaUInt256 ParseHex(string s)
		{
			string h = s.Trim();
			if (h.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || h.StartsWith("0X"))
				h = h.Substring(2);
			if (h.Length == 0 || h.Length > 64)
				throw new ScriptRuntimeException("cannot parse '{0}' as a uint256 hex value (expected 1..64 hex digits)", s);
			BigInteger acc = BigInteger.Zero;
			foreach (char c in h)
			{
				int d;
				if (c >= '0' && c <= '9') d = c - '0';
				else if (c >= 'a' && c <= 'f') d = c - 'a' + 10;
				else if (c >= 'A' && c <= 'F') d = c - 'A' + 10;
				else throw new ScriptRuntimeException("cannot parse '{0}' as a uint256 hex value (invalid digit)", s);
				acc = (acc << 4) + d;
			}
			return new LuaUInt256(acc); // <= 2^256 - 1 by the 64-digit cap
		}

		/// <summary>Returns the canonical lowercase 64-character (zero-padded) hex string.</summary>
		public string ToHex()
		{
			// BigInteger has no fixed-width hex formatter; build 64 nibbles big-endian.
			char[] buf = new char[64];
			BigInteger v = Value;
			for (int i = 63; i >= 0; i--)
			{
				int nib = (int)(v & 0xF);
				buf[i] = (char)(nib < 10 ? '0' + nib : 'a' + (nib - 10));
				v >>= 4;
			}
			return new string(buf);
		}

		#region Arithmetic operators (checked/trapping, width-guarded)

		/// <summary>
		/// Converts a Lua-number operand (a CLR <c>long</c>) to a non-negative BigInteger,
		/// trapping on negative values instead of reinterpreting the bit pattern.
		/// </summary>
		private static BigInteger Operand(long b, string op)
		{
			if (b < 0)
				throw new ScriptRuntimeException("uint256 arithmetic error in '{0}': negative operand ({1})", op, b);
			return new BigInteger(b);
		}

		private static LuaUInt256 Add(BigInteger a, BigInteger b)
		{
			BigInteger r = a + b;
			if (r >= Modulus)
				throw new ScriptRuntimeException("uint256 overflow in '+'");
			return new LuaUInt256(r);
		}

		private static LuaUInt256 Sub(BigInteger a, BigInteger b)
		{
			if (b > a)
				throw new ScriptRuntimeException("uint256 underflow in '-'");
			return new LuaUInt256(a - b);
		}

		private static LuaUInt256 Mul(BigInteger a, BigInteger b)
		{
			BigInteger r = a * b;
			if (r >= Modulus)
				throw new ScriptRuntimeException("uint256 overflow in '*'");
			return new LuaUInt256(r);
		}

		private static LuaUInt256 Div(BigInteger a, BigInteger b)
		{
			if (b.IsZero)
				throw new ScriptRuntimeException("uint256 division by zero");
			return new LuaUInt256(a / b); // floor for non-negative operands
		}

		private static LuaUInt256 Mod(BigInteger a, BigInteger b)
		{
			if (b.IsZero)
				throw new ScriptRuntimeException("uint256 modulo by zero");
			return new LuaUInt256(a % b);
		}

		public static LuaUInt256 operator +(LuaUInt256 a, LuaUInt256 b) { return Add(a.Value, b.Value); }
		public static LuaUInt256 operator +(LuaUInt256 a, long b) { return Add(a.Value, Operand(b, "+")); }
		public static LuaUInt256 operator +(long a, LuaUInt256 b) { return Add(Operand(a, "+"), b.Value); }

		public static LuaUInt256 operator -(LuaUInt256 a, LuaUInt256 b) { return Sub(a.Value, b.Value); }
		public static LuaUInt256 operator -(LuaUInt256 a, long b) { return Sub(a.Value, Operand(b, "-")); }
		public static LuaUInt256 operator -(long a, LuaUInt256 b) { return Sub(Operand(a, "-"), b.Value); }

		public static LuaUInt256 operator *(LuaUInt256 a, LuaUInt256 b) { return Mul(a.Value, b.Value); }
		public static LuaUInt256 operator *(LuaUInt256 a, long b) { return Mul(a.Value, Operand(b, "*")); }
		public static LuaUInt256 operator *(long a, LuaUInt256 b) { return Mul(Operand(a, "*"), b.Value); }

		public static LuaUInt256 operator /(LuaUInt256 a, LuaUInt256 b) { return Div(a.Value, b.Value); }
		public static LuaUInt256 operator /(LuaUInt256 a, long b) { return Div(a.Value, Operand(b, "/")); }
		public static LuaUInt256 operator /(long a, LuaUInt256 b) { return Div(Operand(a, "/"), b.Value); }

		public static LuaUInt256 operator %(LuaUInt256 a, LuaUInt256 b) { return Mod(a.Value, b.Value); }
		public static LuaUInt256 operator %(LuaUInt256 a, long b) { return Mod(a.Value, Operand(b, "%")); }
		public static LuaUInt256 operator %(long a, LuaUInt256 b) { return Mod(Operand(a, "%"), b.Value); }

		// Unary minus on an unsigned value is meaningful only for zero; anything else traps.
		public static LuaUInt256 operator -(LuaUInt256 a)
		{
			if (!a.Value.IsZero)
				throw new ScriptRuntimeException("uint256 underflow in unary '-'");
			return new LuaUInt256(BigInteger.Zero);
		}

		#endregion

		#region Comparison operators

		public static bool operator ==(LuaUInt256 a, LuaUInt256 b) { return a.Value == b.Value; }
		public static bool operator !=(LuaUInt256 a, LuaUInt256 b) { return a.Value != b.Value; }
		public static bool operator <(LuaUInt256 a, LuaUInt256 b) { return a.Value < b.Value; }
		public static bool operator <=(LuaUInt256 a, LuaUInt256 b) { return a.Value <= b.Value; }
		public static bool operator >(LuaUInt256 a, LuaUInt256 b) { return a.Value > b.Value; }
		public static bool operator >=(LuaUInt256 a, LuaUInt256 b) { return a.Value >= b.Value; }

		public static bool operator ==(LuaUInt256 a, long b) { return a.Value == b; }
		public static bool operator ==(long a, LuaUInt256 b) { return a == b.Value; }
		public static bool operator !=(LuaUInt256 a, long b) { return a.Value != b; }
		public static bool operator !=(long a, LuaUInt256 b) { return a != b.Value; }
		public static bool operator <(LuaUInt256 a, long b) { return a.Value < b; }
		public static bool operator <(long a, LuaUInt256 b) { return a < b.Value; }
		public static bool operator <=(LuaUInt256 a, long b) { return a.Value <= b; }
		public static bool operator <=(long a, LuaUInt256 b) { return a <= b.Value; }
		public static bool operator >(LuaUInt256 a, long b) { return a.Value > b; }
		public static bool operator >(long a, LuaUInt256 b) { return a > b.Value; }
		public static bool operator >=(LuaUInt256 a, long b) { return a.Value >= b; }
		public static bool operator >=(long a, LuaUInt256 b) { return a >= b.Value; }

		#endregion

		/// <summary>
		/// Integer square root: the largest <c>r</c> with <c>r*r &lt;= Value</c>. Newton's method
		/// on the exact BigInteger backing (no floating point), so the result is exact at every
		/// magnitude, including the neighbourhoods of perfect squares and 2^256 - 1. The result is
		/// always &lt; 2^128, hence always in range.
		/// </summary>
		public LuaUInt256 Isqrt()
		{
			if (Value.Sign == 0)
				return new LuaUInt256(BigInteger.Zero);
			// Initial estimate: 2^(ceil(bits/2)) is >= the true root, so iteration descends
			// monotonically to floor(sqrt) — the standard integer-Newton convergence.
			int bits = (int)Math.Ceiling(BitLength(Value) / 2.0);
			BigInteger x = BigInteger.One << bits;
			while (true)
			{
				BigInteger y = (x + Value / x) >> 1;
				if (y >= x)
					break;
				x = y;
			}
			// x is now floor(sqrt); guard the boundary explicitly.
			while (x * x > Value) x -= BigInteger.One;
			while ((x + 1) * (x + 1) <= Value) x += BigInteger.One;
			return new LuaUInt256(x);
		}

		private static int BitLength(BigInteger v)
		{
			int len = 0;
			while (v > BigInteger.Zero) { v >>= 1; len++; }
			return len;
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
			if (obj is LuaUInt256) return Value == ((LuaUInt256)obj).Value;
			// Value-equal to any other numeric type (uint256(5) == 5 == uint64(5) == decimal(5)).
			return NumericInterop.AreEqual(Value, obj);
		}

		public bool Equals(LuaUInt256 other)
		{
			return Value == other.Value;
		}

		public int CompareTo(LuaUInt256 other)
		{
			return Value.CompareTo(other.Value);
		}

		/// <summary>
		/// Non-generic comparison used by the runtime to dispatch the ordering metamethods.
		/// Supports comparison against another uint256, any sibling numeric type, or a Lua number.
		/// </summary>
		public int CompareTo(object obj)
		{
			if (obj is LuaUInt256)
				return Value.CompareTo(((LuaUInt256)obj).Value);
			return NumericInterop.Compare(Value, obj);
		}

		public override int GetHashCode()
		{
			// Value-based so it stays consistent with cross-type equality.
			return NumericInterop.ValueHashCode(Value);
		}
	}
}
