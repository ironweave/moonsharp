// Disable warnings about XML documentation
#pragma warning disable 1591

using System.Globalization;
using System.Numerics;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing the 'bigint' arbitrary-precision integer package (a MoonSharp addition).
	///
	/// The module table is callable, so <c>bigint(x)</c> is a shortcut for <c>bigint.new(x)</c>.
	/// Values are <see cref="BigInt"/> userdata supporting the usual arithmetic and comparison
	/// operators (integer division and modulo, as for <see cref="System.Numerics.BigInteger"/>).
	/// </summary>
	[MoonSharpModule(Namespace = "bigint")]
	public class BigIntModule
	{
		public static void MoonSharpInit(Table globalTable, Table bigIntTable)
		{
			UserData.RegisterType<BigInt>();

			// Constants exposed directly as values on the module table.
			bigIntTable.Set("zero", UserData.Create(new BigInt(BigInteger.Zero)));
			bigIntTable.Set("one", UserData.Create(new BigInt(BigInteger.One)));

			// Allow 'bigint(x)' as a shortcut for 'bigint.new(x)'.
			Table meta = new Table(globalTable.OwnerScript);
			meta.Set("__call", DynValue.NewCallback((executionContext, args) =>
			{
				// args[0] is the bigint module table itself (the implicit self of __call);
				// the value to convert is the first real argument.
				return UserData.Create(ToBigInt(args[1], "bigint"));
			}, "bigint"));
			bigIntTable.MetaTable = meta;
		}

		/// <summary>
		/// Coerces a script value (bigint, integer-valued number, or decimal string) into a BigInt.
		/// </summary>
		private static BigInt ToBigInt(DynValue v, string funcName)
		{
			switch (v.Type)
			{
				case DataType.UserData:
					if (v.UserData != null && v.UserData.Object is BigInt)
						return (BigInt)v.UserData.Object;
					throw ScriptRuntimeException.BadArgument(0, funcName, "bigint, number or string expected, got userdata");
				case DataType.Number:
				{
					double d = v.Number;
					if (double.IsNaN(d) || double.IsInfinity(d) || d != System.Math.Floor(d))
						throw new ScriptRuntimeException("bad argument to '{0}' (number has no integer representation)", funcName);
					return new BigInt(new BigInteger(d));
				}
				case DataType.String:
				{
					BigInteger parsed;
					if (!BigInteger.TryParse(v.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
						throw new ScriptRuntimeException("bad argument to '{0}' (cannot parse '{1}' as a bigint)", funcName, v.String);
					return new BigInt(parsed);
				}
				default:
					throw ScriptRuntimeException.BadArgument(0, funcName, "bigint, number or string expected, got " + v.Type.ToLuaTypeString());
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue @new(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToBigInt(args[0], "bigint.new"));
		}

		[MoonSharpModuleMethod]
		public static DynValue parse(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue s = args.AsType(0, "bigint.parse", DataType.String, false);
			BigInteger parsed;
			if (!BigInteger.TryParse(s.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("bad argument to 'bigint.parse' (cannot parse '{0}' as a bigint)", s.String);
			return UserData.Create(new BigInt(parsed));
		}

		[MoonSharpModuleMethod]
		public static DynValue tostring(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewString(ToBigInt(args[0], "bigint.tostring").ToString());
		}

		[MoonSharpModuleMethod]
		public static DynValue tonumber(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewNumber(ToBigInt(args[0], "bigint.tonumber").ToNumber());
		}

		[MoonSharpModuleMethod]
		public static DynValue abs(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToBigInt(args[0], "bigint.abs").Abs());
		}

		[MoonSharpModuleMethod]
		public static DynValue pow(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			BigInt b = ToBigInt(args[0], "bigint.pow");
			DynValue e = args.AsType(1, "bigint.pow", DataType.Number, false);
			double exp = e.Number;
			// Reject negative/non-integer exponents and bound to int range so the (int) cast
			// below is well-defined. This does NOT bound compute/memory cost (int.MaxValue is
			// still astronomical) — actual resource limiting is the host engine's step/gas
			// metering, not this type.
			if (exp < 0 || exp != System.Math.Floor(exp) || exp > int.MaxValue)
				throw new ScriptRuntimeException("bad argument #2 to 'bigint.pow' (non-negative integer exponent expected)");
			return UserData.Create(b.Pow((int)exp));
		}
	}
}
