using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class UInt64Tests
	{
		private static string Str(string code) { return Script.RunString(code).String; }
		private static bool Bool(string code) { return Script.RunString(code).Boolean; }

		[Test]
		public void UInt64_Construct()
		{
			Assert.AreEqual("42", Str("return tostring(uint64(42))"));
			Assert.AreEqual("18446744073709551615", Str("return tostring(uint64('18446744073709551615'))"));
			Assert.AreEqual("100", Str("return tostring(uint64.new(100))"));
			Assert.AreEqual("100", Str("return tostring(uint64.parse('100'))"));
		}

		[Test]
		public void UInt64_Arithmetic()
		{
			Assert.AreEqual("5", Str("return tostring(uint64(2) + uint64(3))"));
			Assert.AreEqual("42", Str("return tostring(uint64(6) * uint64(7))"));
			Assert.AreEqual("3", Str("return tostring(uint64(7) / uint64(2))"));
			Assert.AreEqual("1", Str("return tostring(uint64(7) % uint64(2))"));
		}

		[Test]
		public void UInt64_TrapsOnOverflow()
		{
			// Checked arithmetic: overflow/underflow raises instead of silently wrapping.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(0) - 1"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64('18446744073709551615') + 1"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64('18446744073709551615') * 2"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(1) / uint64(0)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(10) + (-5)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return -uint64(1)"));
		}

		[Test]
		public void UInt64_MixedWithLuaNumbers()
		{
			Assert.AreEqual("15", Str("return tostring(uint64(10) + 5)"));
			Assert.AreEqual("15", Str("return tostring(5 + uint64(10))"));
			Assert.AreEqual("30", Str("return tostring(uint64(10) * 3)"));
		}

		[Test]
		public void UInt64_Comparisons()
		{
			Assert.IsTrue(Bool("return uint64(5) < uint64(10)"));
			Assert.IsTrue(Bool("return uint64(10) > uint64(5)"));
			Assert.IsTrue(Bool("return uint64(5) == uint64(5)"));
			Assert.IsTrue(Bool("return uint64(5) < 10"));
			// any uint64 is >= 0, so it is greater than any negative Lua number
			Assert.IsTrue(Bool("return uint64(0) > -1"));
			// huge value exceeding the exact range of a double still compares correctly
			Assert.IsTrue(Bool("return uint64('18446744073709551615') > uint64('18446744073709551614')"));
			// Mixed equality with a plain Lua number (both operand orders).
			Assert.IsTrue(Bool("return uint64(5) == 5"));
			Assert.IsTrue(Bool("return 5 == uint64(5)"));
			Assert.IsFalse(Bool("return uint64(5) == 6"));
			Assert.IsTrue(Bool("return uint64(5) ~= 6"));
		}

		[Test]
		public void UInt64_HelpersAndConstants()
		{
			Assert.AreEqual(123.0, Script.RunString("return uint64.tonumber(uint64(123))").Number);
			Assert.AreEqual("18446744073709551615", Str("return tostring(uint64.max)"));
			Assert.AreEqual("0", Str("return tostring(uint64.min)"));
		}

		[Test]
		public void UInt64_FromInt64()
		{
			Assert.AreEqual("5", Str("return tostring(uint64(int64(5)))"));
		}

		[Test]
		public void UInt64_BadInputRaises()
		{
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(-1)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(int64(-1))"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64('nope')"));
		}

		[Test]
		public void UInt64_NoRawClrExceptionsEscapeToScript()
		{
			// These previously escaped as raw FormatException/OverflowException/ArgumentException,
			// which pcall cannot catch and which crash the embedding host.
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64.max.Parse('zz')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64.max.Parse('-1')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(1) < 'abc'"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64.max.CompareTo({})"));
			Assert.IsFalse(Bool("return (pcall(function () return uint64(1) < 'abc' end))"));

			// Error message reports the operand in Lua type terms, not CLR type names.
			var ex = Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return uint64(1) < 'abc'"));
			StringAssert.Contains("string", ex.Message);
			StringAssert.DoesNotContain("String", ex.Message);
		}
	}
}
