using System.Numerics;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class UInt256Tests
	{
		private static string Str(string code) { return Script.RunString(code).String; }
		private static bool Bool(string code) { return Script.RunString(code).Boolean; }

		private const string Max = "115792089237316195423570985008687907853269984665640564039457584007913129639935"; // 2^256 - 1

		[Test]
		public void UInt256_Construct()
		{
			Assert.AreEqual("42", Str("return tostring(uint256(42))"));
			Assert.AreEqual(Max, Str("return tostring(uint256('" + Max + "'))"));
			Assert.AreEqual("100", Str("return tostring(uint256.new(100))"));
			Assert.AreEqual("250", Str("return tostring(uint256.parse('250'))"));
			Assert.AreEqual("5", Str("return tostring(uint256(uint64(5)))"));
			Assert.AreEqual("7", Str("return tostring(uint256(int64(7)))"));
			Assert.AreEqual("9", Str("return tostring(uint256(bigint(9)))"));
		}

		[Test]
		public void UInt256_WideArithmetic_BeyondUInt64()
		{
			// 2^64 * 2^64 = 2^128 — overflows uint64, exact in uint256.
			Assert.AreEqual("340282366920938463463374607431768211456",
				Str("return tostring(uint256('18446744073709551616') * uint256('18446744073709551616'))"));
			Assert.AreEqual("6", Str("return tostring(uint256(10) - uint256(4))"));
			Assert.AreEqual("3", Str("return tostring(uint256(7) / uint256(2))"));
			Assert.AreEqual("1", Str("return tostring(uint256(7) % uint256(2))"));
		}

		[Test]
		public void UInt256_TrapsOnOverflowUnderflowDivZero()
		{
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256('" + Max + "') + 1"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256(0) - 1"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256('" + Max + "') * 2"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256(1) / uint256(0)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256(1) % uint256(0)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256(10) + (-5)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return -uint256(1)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256(-1)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256('nope')"));
		}

		[Test]
		public void UInt256_Isqrt_ExactAtBoundaries()
		{
			Assert.AreEqual("0", Str("return tostring(uint256.isqrt(uint256(0)))"));
			Assert.AreEqual("4", Str("return tostring(uint256.isqrt(uint256(16)))"));
			Assert.AreEqual("4", Str("return tostring(uint256.isqrt(uint256(17)))"));
			Assert.AreEqual("3", Str("return tostring(uint256.isqrt(uint256(15)))"));
			// isqrt(2^256 - 1) = 2^128 - 1
			Assert.AreEqual("340282366920938463463374607431768211455",
				Str("return tostring(uint256.isqrt(uint256('" + Max + "')))"));
		}

		[Test]
		public void UInt256_HexCodec()
		{
			Assert.AreEqual(new string('0', 64), Str("return uint256.tohex(uint256(0))"));
			Assert.AreEqual(new string('0', 62) + "ff", Str("return uint256.tohex(uint256(255))"));
			Assert.AreEqual("255", Str("return tostring(uint256.fromhex('ff'))"));
			Assert.AreEqual("255", Str("return tostring(uint256.fromhex('0xFF'))"));
			Assert.AreEqual(new string('f', 64), Str("return uint256.tohex(uint256.fromhex('" + new string('f', 64) + "'))"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256.fromhex('xyz')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint256.fromhex('" + new string('f', 65) + "')"));
		}

		[Test]
		public void UInt256_CrossTypeEqualityAndOrdering()
		{
			Assert.IsTrue(Bool("return uint256(5) == 5"));
			Assert.IsTrue(Bool("return uint256(5) == uint64(5)"));
			Assert.IsTrue(Bool("return uint256(5) == bigint(5)"));
			Assert.IsTrue(Bool("return uint256(5) == int64(5)"));
			Assert.IsTrue(Bool("return uint256(6) >= uint64(5)"));
			Assert.IsTrue(Bool("return bigint(6) > uint256(5)"));
			Assert.IsTrue(Bool("return uint256(5) < 10"));
			Assert.IsTrue(Bool("return uint256(0) > -1"));
		}

		[Test]
		public void UInt256_NarrowsToUInt64_TrappingOutOfRange()
		{
			Assert.AreEqual("123", Str("return tostring(uint64(uint256(123)))"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(uint256('" + Max + "'))"));
		}

		[Test]
		public void UInt256_TableKeyIdentity_DistinctFromSiblings()
		{
			// Cross-type VALUE equality does not collapse table-key identity: a uint256 key
			// is a distinct slot from a plain number / uint64 / bigint key (stock Lua rawget
			// short-circuits on type before ever consulting __eq).
			Assert.IsTrue(Bool(@"
				local t = {}
				t[uint256(5)] = 'a'
				t[5] = 'b'
				t[uint64(5)] = 'c'
				return t[uint256(5)] == 'a' and t[5] == 'b' and t[uint64(5)] == 'c'
			"));
		}

		[Test]
		public void UInt256_Constants()
		{
			Assert.AreEqual(Max, Str("return tostring(uint256.max)"));
			Assert.AreEqual("0", Str("return tostring(uint256.min)"));
		}

		[Test]
		public void UInt256_DifferentialAgainstBigInteger()
		{
			// The type's only bespoke logic beyond BigInteger is the range guard and isqrt;
			// this pins add/sub/mul/div/mod/isqrt against a BigInteger reference over
			// deterministic vectors chosen to hit carries, boundaries, and the fee-shaped
			// ~156-bit product the AMM relies on. Deterministic seed — no RNG in a test that
			// documents required behavior.
			BigInteger mod = BigInteger.One << 256;
			BigInteger[] vals =
			{
				BigInteger.Zero, BigInteger.One, new BigInteger(2), new BigInteger(ulong.MaxValue),
				BigInteger.One << 64, BigInteger.One << 128, BigInteger.One << 200,
				mod - 1, mod - 2, new BigInteger(1_000_000_007),
				BigInteger.Parse("340282366920938463463374607431768211455"), // 2^128 - 1
				BigInteger.Parse("99999999999999999999999999999999999999"),
			};

			foreach (var a in vals)
			foreach (var b in vals)
			{
				string sa = a.ToString(), sb = b.ToString();

				// add (only when it stays in range — otherwise it must trap, covered elsewhere)
				if (a + b < mod)
					Assert.AreEqual((a + b).ToString(), Str($"return tostring(uint256('{sa}') + uint256('{sb}'))"), $"{sa}+{sb}");
				// sub (only when non-negative)
				if (a >= b)
					Assert.AreEqual((a - b).ToString(), Str($"return tostring(uint256('{sa}') - uint256('{sb}'))"), $"{sa}-{sb}");
				// mul (only when it stays in range)
				if (a * b < mod)
					Assert.AreEqual((a * b).ToString(), Str($"return tostring(uint256('{sa}') * uint256('{sb}'))"), $"{sa}*{sb}");
				// div / mod (only for non-zero divisor)
				if (!b.IsZero)
				{
					Assert.AreEqual((a / b).ToString(), Str($"return tostring(uint256('{sa}') / uint256('{sb}'))"), $"{sa}/{sb}");
					Assert.AreEqual((a % b).ToString(), Str($"return tostring(uint256('{sa}') % uint256('{sb}'))"), $"{sa}%{sb}");
				}
				// isqrt of each value equals the integer square root
				BigInteger r = ISqrtRef(a);
				Assert.AreEqual(r.ToString(), Str($"return tostring(uint256.isqrt(uint256('{sa}')))"), $"isqrt({sa})");
				Assert.IsTrue(r * r <= a && (r + 1) * (r + 1) > a, $"isqrt invariant {sa}");
			}
		}

		private static BigInteger ISqrtRef(BigInteger n)
		{
			if (n.Sign <= 0) return BigInteger.Zero;
			BigInteger x = n, y = (x + 1) >> 1;
			while (y < x) { x = y; y = (x + n / x) >> 1; }
			return x;
		}
	}
}
