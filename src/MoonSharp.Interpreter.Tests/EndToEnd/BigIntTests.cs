using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class BigIntTests
	{
		private static string Str(string code)
		{
			return Script.RunString(code).String;
		}

		private static bool Bool(string code)
		{
			return Script.RunString(code).Boolean;
		}

		[Test]
		public void BigInt_ConstructFromNumberAndString()
		{
			Assert.AreEqual("42", Str("return tostring(bigint(42))"));
			Assert.AreEqual("-7", Str("return tostring(bigint(-7))"));
			Assert.AreEqual("123456789012345678901234567890",
				Str("return tostring(bigint('123456789012345678901234567890'))"));
		}

		[Test]
		public void BigInt_NewAndParse()
		{
			Assert.AreEqual("100", Str("return tostring(bigint.new(100))"));
			Assert.AreEqual("100", Str("return tostring(bigint.parse('100'))"));
		}

		[Test]
		public void BigInt_AdditionBeyondDoublePrecision()
		{
			// 2^63 + 1 cannot be represented exactly by a double; bigint must keep it exact.
			Assert.AreEqual("9223372036854775809",
				Str("return tostring(bigint('9223372036854775808') + bigint(1))"));
		}

		[Test]
		public void BigInt_Multiplication()
		{
			// Exact product well beyond 64-bit range.
			Assert.AreEqual("1000000000000000000000000",
				Str("return tostring(bigint('1000000000000') * bigint('1000000000000'))"));
			// 2^100 via pow
			Assert.AreEqual("1267650600228229401496703205376",
				Str("return tostring(bigint.pow(bigint(2), 100))"));
		}

		[Test]
		public void BigInt_Subtraction()
		{
			Assert.AreEqual("-1", Str("return tostring(bigint(9) - bigint(10))"));
		}

		[Test]
		public void BigInt_IntegerDivisionAndModulo()
		{
			Assert.AreEqual("3", Str("return tostring(bigint(7) / bigint(2))"));
			Assert.AreEqual("1", Str("return tostring(bigint(7) % bigint(2))"));
		}

		[Test]
		public void BigInt_UnaryMinus()
		{
			Assert.AreEqual("-5", Str("return tostring(-bigint(5))"));
		}

		[Test]
		public void BigInt_MixedWithLuaNumbers()
		{
			Assert.AreEqual("15", Str("return tostring(bigint(10) + 5)"));
			Assert.AreEqual("15", Str("return tostring(5 + bigint(10))"));
			Assert.AreEqual("30", Str("return tostring(bigint(10) * 3)"));
		}

		[Test]
		public void BigInt_Comparisons()
		{
			Assert.IsTrue(Bool("return bigint(5) < bigint(10)"));
			Assert.IsTrue(Bool("return bigint(10) > bigint(5)"));
			Assert.IsTrue(Bool("return bigint(5) <= bigint(5)"));
			Assert.IsTrue(Bool("return bigint(5) == bigint(5)"));
			Assert.IsFalse(Bool("return bigint(5) == bigint(6)"));
			Assert.IsTrue(Bool("return bigint(5) < 10"));
			// Mixed equality with a plain Lua number (both operand orders).
			Assert.IsTrue(Bool("return bigint(5) == 5"));
			Assert.IsTrue(Bool("return 5 == bigint(5)"));
			Assert.IsFalse(Bool("return bigint(5) == 6"));
			Assert.IsTrue(Bool("return bigint(5) ~= 6"));
		}

		[Test]
		public void BigInt_AbsAndPow()
		{
			Assert.AreEqual("17", Str("return tostring(bigint.abs(bigint(-17)))"));
			Assert.AreEqual("1024", Str("return tostring(bigint.pow(bigint(2), 10))"));
			// Negative / oversized exponents are rejected, not silently mis-cast.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return bigint.pow(bigint(2), -1)"));
		}

		[Test]
		public void BigInt_DivideByZeroTraps()
		{
			// Arbitrary-precision so it cannot overflow, but /0 and %0 must trap rather
			// than escaping as a raw CLR DivideByZeroException.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return bigint(1) / bigint(0)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return bigint(1) % bigint(0)"));
		}

		[Test]
		public void BigInt_ToNumber()
		{
			Assert.AreEqual(123.0, Script.RunString("return bigint.tonumber(bigint(123))").Number);
		}

		[Test]
		public void BigInt_Constants()
		{
			Assert.AreEqual("0", Str("return tostring(bigint.zero)"));
			Assert.AreEqual("1", Str("return tostring(bigint.one)"));
		}
	}
}
