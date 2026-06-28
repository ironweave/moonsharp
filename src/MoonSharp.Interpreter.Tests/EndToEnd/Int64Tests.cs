using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class Int64Tests
	{
		private static string Str(string code) { return Script.RunString(code).String; }
		private static bool Bool(string code) { return Script.RunString(code).Boolean; }

		[Test]
		public void Int64_Construct()
		{
			Assert.AreEqual("42", Str("return tostring(int64(42))"));
			Assert.AreEqual("-7", Str("return tostring(int64(-7))"));
			Assert.AreEqual("9223372036854775807", Str("return tostring(int64('9223372036854775807'))"));
			Assert.AreEqual("100", Str("return tostring(int64.new(100))"));
			Assert.AreEqual("100", Str("return tostring(int64.parse('100'))"));
		}

		[Test]
		public void Int64_Arithmetic()
		{
			Assert.AreEqual("5", Str("return tostring(int64(2) + int64(3))"));
			Assert.AreEqual("-1", Str("return tostring(int64(9) - int64(10))"));
			Assert.AreEqual("42", Str("return tostring(int64(6) * int64(7))"));
			Assert.AreEqual("3", Str("return tostring(int64(7) / int64(2))"));
			Assert.AreEqual("1", Str("return tostring(int64(7) % int64(2))"));
			Assert.AreEqual("-5", Str("return tostring(-int64(5))"));
		}

		[Test]
		public void Int64_WrapsAround()
		{
			// int64.max + 1 wraps to int64.min
			Assert.AreEqual("-9223372036854775808",
				Str("return tostring(int64('9223372036854775807') + 1)"));
		}

		[Test]
		public void Int64_MixedWithLuaNumbers()
		{
			Assert.AreEqual("15", Str("return tostring(int64(10) + 5)"));
			Assert.AreEqual("15", Str("return tostring(5 + int64(10))"));
			Assert.AreEqual("30", Str("return tostring(int64(10) * 3)"));
			Assert.AreEqual("3", Str("return tostring(int64(10) / 3)"));
		}

		[Test]
		public void Int64_Comparisons()
		{
			Assert.IsTrue(Bool("return int64(5) < int64(10)"));
			Assert.IsTrue(Bool("return int64(10) > int64(5)"));
			Assert.IsTrue(Bool("return int64(5) <= int64(5)"));
			Assert.IsTrue(Bool("return int64(5) == int64(5)"));
			Assert.IsFalse(Bool("return int64(5) == int64(6)"));
			Assert.IsTrue(Bool("return int64(5) < 10"));
			Assert.IsTrue(Bool("return int64(-3) < 0"));
		}

		[Test]
		public void Int64_HelpersAndConstants()
		{
			Assert.AreEqual("17", Str("return tostring(int64.abs(int64(-17)))"));
			Assert.AreEqual(123.0, Script.RunString("return int64.tonumber(int64(123))").Number);
			Assert.AreEqual("9223372036854775807", Str("return tostring(int64.max)"));
			Assert.AreEqual("-9223372036854775808", Str("return tostring(int64.min)"));
		}

		[Test]
		public void Int64_FromUInt64()
		{
			Assert.AreEqual("5", Str("return tostring(int64(uint64(5)))"));
		}

		[Test]
		public void Int64_OutOfRangeOrBadInputRaises()
		{
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return int64(2.5)"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return int64('nope')"));
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString("return int64(uint64('18446744073709551615'))"));
		}
	}
}
