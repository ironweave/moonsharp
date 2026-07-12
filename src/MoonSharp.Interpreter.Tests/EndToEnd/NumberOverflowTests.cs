using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	/// <summary>
	/// IronContract numeric hygiene for the default 'number' (double) type: arithmetic
	/// operators trap when a finite pair of operands would yield a non-finite result,
	/// instead of silently producing inf/nan. This is a deliberate departure from stock Lua.
	///
	/// Boundary (by design, operator-level): a non-finite value that *enters* from a math
	/// library function (e.g. math.exp(1000), math.sqrt(-1)) or from tonumber('1e400') is not
	/// caught here, because it is already non-finite as an operand. Contracts use no math.*
	/// today; tightening this is tracked separately.
	/// </summary>
	[TestFixture]
	public class NumberOverflowTests
	{
		private static void AssertTraps(string code)
		{
			Assert.Throws<ScriptRuntimeException>(() => Script.RunString(code), code);
		}

		[Test]
		public void DivisionAndModuloByZeroTrap()
		{
			AssertTraps("return 1 / 0");
			AssertTraps("return -1 / 0");
			AssertTraps("return 0 / 0");   // nan
			AssertTraps("return 1 % 0");   // nan
			AssertTraps("return 5.5 % 0");
		}

		[Test]
		public void OverflowToInfinityTraps()
		{
			AssertTraps("return 1e308 * 10");
			AssertTraps("return 1e308 + 1e308");
			AssertTraps("return -1e308 - 1e308");
			AssertTraps("return 2 ^ 2000");
		}

		[Test]
		public void FiniteArithmeticIsUnaffected()
		{
			Assert.AreEqual(1024.0, Script.RunString("return 2 ^ 10").Number);
			Assert.AreEqual(-14.0, Script.RunString("return -7 / 0.5").Number);
			Assert.AreEqual(2.0, Script.RunString("return -25 % 3").Number, 0.0);
			Assert.AreEqual(3.5, Script.RunString("return 1.5 + 2").Number);
		}

		[Test]
		public void MathHugeRemainsUsableAsSentinel()
		{
			// The constant itself is a number and stays comparable/usable: only a genuine
			// finite -> non-finite transition traps, and math.huge is already non-finite.
			Assert.AreEqual("number", Script.RunString("return type(math.huge)").String);
			Assert.IsTrue(Script.RunString("return math.huge > 1e300").Boolean);
			Assert.IsTrue(Script.RunString("return math.huge == math.huge + 1").Boolean);
			Assert.IsFalse(Script.RunString("return 5 > math.huge").Boolean);
		}
	}
}
