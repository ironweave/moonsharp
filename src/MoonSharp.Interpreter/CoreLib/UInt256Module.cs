// Disable warnings about XML documentation
#pragma warning disable 1591

using System.Globalization;
using System.Numerics;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing the 'uint256' fixed-width unsigned 256-bit integer package (a MoonSharp addition).
	///
	/// The module table is callable, so <c>uint256(x)</c> is a shortcut for <c>uint256.new(x)</c>.
	/// Values are <see cref="LuaUInt256"/> userdata supporting the usual arithmetic and comparison
	/// operators with checked (trapping) semantics: overflow past 2^256, underflow, and division by
	/// zero raise a script error. The type carries wide intermediate quantities for consensus math
	/// (constant-product invariants, share bootstraps); it never wraps or truncates silently.
	/// </summary>
	[MoonSharpModule(Namespace = "uint256")]
	public class UInt256Module
	{
		public static void MoonSharpInit(Table globalTable, Table moduleTable)
		{
			UserData.RegisterType<LuaUInt256>();

			moduleTable.Set("min", UserData.Create(new LuaUInt256(BigInteger.Zero)));
			moduleTable.Set("max", UserData.Create(new LuaUInt256(LuaUInt256.MaxValue)));

			// Allow 'uint256(x)' as a shortcut for 'uint256.new(x)'.
			Table meta = new Table(globalTable.OwnerScript);
			meta.Set("__call", DynValue.NewCallback((executionContext, args) =>
				UserData.Create(ToUInt256(args[1], "uint256")), "uint256"));
			moduleTable.MetaTable = meta;
		}

		/// <summary>
		/// Coerces a script value (uint256, uint64, non-negative int64, non-negative
		/// integer-valued number, bigint in range, or decimal string) into a LuaUInt256.
		/// </summary>
		private static LuaUInt256 ToUInt256(DynValue v, string funcName)
		{
			switch (v.Type)
			{
				case DataType.UserData:
					if (v.UserData != null && v.UserData.Object is LuaUInt256)
						return (LuaUInt256)v.UserData.Object;
					if (v.UserData != null && v.UserData.Object is LuaUInt64)
						return new LuaUInt256(new BigInteger(((LuaUInt64)v.UserData.Object).Value));
					if (v.UserData != null && v.UserData.Object is LuaInt64)
					{
						long l = ((LuaInt64)v.UserData.Object).Value;
						if (l < 0)
							throw new ScriptRuntimeException("bad argument to '{0}' (int64 value {1} is negative and cannot be a uint256)", funcName, l);
						return new LuaUInt256(new BigInteger(l));
					}
					if (v.UserData != null && v.UserData.Object is BigInt)
					{
						BigInteger b = ((BigInt)v.UserData.Object).Value;
						if (b.Sign < 0 || b >= LuaUInt256.Modulus)
							throw new ScriptRuntimeException("bad argument to '{0}' (bigint value is out of range for uint256)", funcName);
						return new LuaUInt256(b);
					}
					throw ScriptRuntimeException.BadArgument(0, funcName, "uint256, uint64, int64, bigint, number or string expected, got userdata");
				case DataType.Number:
				{
					double d = v.Number;
					if (double.IsNaN(d) || double.IsInfinity(d) || d != System.Math.Floor(d))
						throw new ScriptRuntimeException("bad argument to '{0}' (number has no integer representation)", funcName);
					if (d < 0.0)
						throw new ScriptRuntimeException("bad argument to '{0}' (number is out of range for uint256)", funcName);
					return new LuaUInt256(new BigInteger(d));
				}
				case DataType.String:
				{
					BigInteger parsed;
					if (!BigInteger.TryParse(v.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
						throw new ScriptRuntimeException("bad argument to '{0}' (cannot parse '{1}' as a uint256)", funcName, v.String);
					if (parsed.Sign < 0 || parsed >= LuaUInt256.Modulus)
						throw new ScriptRuntimeException("bad argument to '{0}' (value '{1}' is out of range for uint256)", funcName, v.String);
					return new LuaUInt256(parsed);
				}
				default:
					throw ScriptRuntimeException.BadArgument(0, funcName, "uint256, uint64, int64, bigint, number or string expected, got " + v.Type.ToLuaTypeString());
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue @new(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToUInt256(args[0], "uint256.new"));
		}

		[MoonSharpModuleMethod]
		public static DynValue parse(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue s = args.AsType(0, "uint256.parse", DataType.String, false);
			return UserData.Create(LuaUInt256.Parse(s.String.Trim()));
		}

		[MoonSharpModuleMethod]
		public static DynValue fromhex(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue s = args.AsType(0, "uint256.fromhex", DataType.String, false);
			return UserData.Create(LuaUInt256.ParseHex(s.String));
		}

		[MoonSharpModuleMethod]
		public static DynValue tohex(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewString(ToUInt256(args[0], "uint256.tohex").ToHex());
		}

		[MoonSharpModuleMethod]
		public static DynValue tostring(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewString(ToUInt256(args[0], "uint256.tostring").ToString());
		}

		[MoonSharpModuleMethod]
		public static DynValue tonumber(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewNumber(ToUInt256(args[0], "uint256.tonumber").ToNumber());
		}

		[MoonSharpModuleMethod]
		public static DynValue isqrt(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToUInt256(args[0], "uint256.isqrt").Isqrt());
		}
	}
}
