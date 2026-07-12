using System.Collections.Generic;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	/// <summary>
	/// Cross-type value semantics for the numeric userdata types (int64, uint64, bigint,
	/// decimal) and plain Lua numbers. Equal mathematical values compare equal and order
	/// consistently regardless of which concrete type carries them.
	/// </summary>
	[TestFixture]
	public class NumericCrossTypeTests
	{
		private static bool Bool(string code)
		{
			return Script.RunString(code).Boolean;
		}

		[Test]
		public void CrossType_EqualityMatrix()
		{
			// Every pairing of the four numeric userdata types at value 5 is equal.
			string[] types = { "int64", "uint64", "bigint", "decimal" };
			foreach (string a in types)
				foreach (string b in types)
					Assert.IsTrue(Bool("return " + a + "(5) == " + b + "(5)"),
						a + "(5) == " + b + "(5) should be true");
		}

		[Test]
		public void CrossType_Inequality()
		{
			Assert.IsFalse(Bool("return int64(5) == uint64(6)"));
			Assert.IsFalse(Bool("return decimal('5.5') == bigint(5)"));
			Assert.IsFalse(Bool("return int64(-1) == uint64(0)"));
			// A fractional decimal never equals an integer sibling.
			Assert.IsFalse(Bool("return decimal('5.5') == int64(5)"));
		}

		[Test]
		public void CrossType_Ordering()
		{
			Assert.IsTrue(Bool("return int64(5) < decimal(6)"));
			Assert.IsTrue(Bool("return uint64(5) < bigint(6)"));
			Assert.IsTrue(Bool("return bigint(6) >= uint64(5)"));
			Assert.IsTrue(Bool("return decimal('5.5') > int64(5)"));
			Assert.IsTrue(Bool("return int64(5) <= decimal(5)"));
		}

		[Test]
		public void CrossType_EqualityWithNonNumericIsFalse()
		{
			Assert.IsFalse(Bool("return uint64(5) == 'x'"));
			Assert.IsFalse(Bool("return decimal(5) == {}"));
			Assert.IsFalse(Bool("return bigint(5) == nil"));
			Assert.IsFalse(Bool("return int64(5) == true"));
		}

		[Test]
		public void CrossType_TableKeysStayDistinct()
		{
			// '==' now spans types, but table keys use raw equality (as in stock Lua):
			// a number key, and each numeric-userdata key, remain distinct slots.
			Assert.AreEqual("n/u/i/d", Script.RunString(@"
				local t = {}
				t[5]          = 'n'
				t[uint64(5)]  = 'u'
				t[int64(5)]   = 'i'
				t[decimal(5)] = 'd'
				return t[5] .. '/' .. t[uint64(5)] .. '/' .. t[int64(5)] .. '/' .. t[decimal(5)]
			").String);
		}

		[Test]
		public void CrossType_EqualsAndHashCodeContract()
		{
			// The four numeric userdata types at the same value satisfy the CLR equality
			// contract: pairwise-equal (symmetric) and equal hash codes, so they behave as a
			// single key in a hash-based container.
			object[] fives =
			{
				new LuaInt64(5), new LuaUInt64(5), new BigInt(5), new DecimalType(5m)
			};

			for (int i = 0; i < fives.Length; i++)
			{
				for (int j = 0; j < fives.Length; j++)
				{
					Assert.IsTrue(fives[i].Equals(fives[j]), fives[i] + ".Equals(" + fives[j] + ")");
					Assert.AreEqual(fives[i].GetHashCode(), fives[j].GetHashCode(),
						"hash " + fives[i] + " vs " + fives[j]);
				}
				// Hash also matches the plain double representation these values equal.
				Assert.AreEqual(5.0.GetHashCode(), fives[i].GetHashCode(), "hash vs double 5.0");
			}

			Assert.AreEqual(1, new HashSet<object>(fives).Count, "collapse to one hash-set entry");
		}

		[Test]
		public void CrossType_HugeMagnitudesCompareExactly()
		{
			// uint64 max exceeds a double's exact range; bigint carries it precisely and the
			// two compare equal without falling back to lossy double space.
			Assert.IsTrue(Bool("return uint64('18446744073709551615') == bigint('18446744073709551615')"));
			Assert.IsFalse(Bool("return uint64('18446744073709551615') == bigint('18446744073709551614')"));
		}
	}
}
