// Disable warnings about XML documentation
#pragma warning disable 1591

using System.Globalization;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing the 'uint64' fixed-width unsigned 64-bit integer package (a MoonSharp addition).
	///
	/// The module table is callable, so <c>uint64(x)</c> is a shortcut for <c>uint64.new(x)</c>.
	/// Values are <see cref="LuaUInt64"/> userdata supporting the usual arithmetic and comparison
	/// operators with checked (trapping) semantics: overflow, underflow, and division by zero raise a script error.
	/// </summary>
	[MoonSharpModule(Namespace = "uint64")]
	public class UInt64Module
	{
		public static void MoonSharpInit(Table globalTable, Table moduleTable)
		{
			UserData.RegisterType<LuaUInt64>();

			moduleTable.Set("min", UserData.Create(new LuaUInt64(ulong.MinValue)));
			moduleTable.Set("max", UserData.Create(new LuaUInt64(ulong.MaxValue)));

			// Allow 'uint64(x)' as a shortcut for 'uint64.new(x)'.
			Table meta = new Table(globalTable.OwnerScript);
			meta.Set("__call", DynValue.NewCallback((executionContext, args) =>
				UserData.Create(ToUInt64(args[1], "uint64")), "uint64"));
			moduleTable.MetaTable = meta;
		}

		/// <summary>
		/// Coerces a script value (uint64, non-negative int64, non-negative integer-valued number,
		/// or decimal string) into a LuaUInt64.
		/// </summary>
		private static LuaUInt64 ToUInt64(DynValue v, string funcName)
		{
			switch (v.Type)
			{
				case DataType.UserData:
					if (v.UserData != null && v.UserData.Object is LuaUInt64)
						return (LuaUInt64)v.UserData.Object;
					if (v.UserData != null && v.UserData.Object is LuaInt64)
					{
						long l = ((LuaInt64)v.UserData.Object).Value;
						if (l < 0)
							throw new ScriptRuntimeException("bad argument to '{0}' (int64 value {1} is negative and cannot be a uint64)", funcName, l);
						return new LuaUInt64((ulong)l);
					}
					throw ScriptRuntimeException.BadArgument(0, funcName, "uint64, int64, number or string expected, got userdata");
				case DataType.Number:
				{
					double d = v.Number;
					if (double.IsNaN(d) || double.IsInfinity(d) || d != System.Math.Floor(d))
						throw new ScriptRuntimeException("bad argument to '{0}' (number has no integer representation)", funcName);
					if (d < 0.0 || d >= 18446744073709551616.0)
						throw new ScriptRuntimeException("bad argument to '{0}' (number is out of range for uint64)", funcName);
					return new LuaUInt64((ulong)d);
				}
				case DataType.String:
				{
					ulong parsed;
					if (!ulong.TryParse(v.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
						throw new ScriptRuntimeException("bad argument to '{0}' (cannot parse '{1}' as a uint64)", funcName, v.String);
					return new LuaUInt64(parsed);
				}
				default:
					throw ScriptRuntimeException.BadArgument(0, funcName, "uint64, int64, number or string expected, got " + v.Type.ToLuaTypeString());
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue @new(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToUInt64(args[0], "uint64.new"));
		}

		[MoonSharpModuleMethod]
		public static DynValue parse(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue s = args.AsType(0, "uint64.parse", DataType.String, false);
			ulong parsed;
			if (!ulong.TryParse(s.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("bad argument to 'uint64.parse' (cannot parse '{0}' as a uint64)", s.String);
			return UserData.Create(new LuaUInt64(parsed));
		}

		[MoonSharpModuleMethod]
		public static DynValue tostring(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewString(ToUInt64(args[0], "uint64.tostring").ToString());
		}

		[MoonSharpModuleMethod]
		public static DynValue tonumber(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewNumber(ToUInt64(args[0], "uint64.tonumber").ToNumber());
		}
	}
}
