// Disable warnings about XML documentation
#pragma warning disable 1591

using System.Globalization;

namespace MoonSharp.Interpreter.CoreLib
{
	/// <summary>
	/// Class implementing the 'int64' fixed-width signed 64-bit integer package (a MoonSharp addition).
	///
	/// The module table is callable, so <c>int64(x)</c> is a shortcut for <c>int64.new(x)</c>.
	/// Values are <see cref="LuaInt64"/> userdata supporting the usual arithmetic and comparison
	/// operators with wraparound (unchecked) semantics, like .NET <c>long</c>.
	/// </summary>
	[MoonSharpModule(Namespace = "int64")]
	public class Int64Module
	{
		public static void MoonSharpInit(Table globalTable, Table moduleTable)
		{
			UserData.RegisterType<LuaInt64>();

			moduleTable.Set("min", UserData.Create(new LuaInt64(long.MinValue)));
			moduleTable.Set("max", UserData.Create(new LuaInt64(long.MaxValue)));

			// Allow 'int64(x)' as a shortcut for 'int64.new(x)'.
			Table meta = new Table(globalTable.OwnerScript);
			meta.Set("__call", DynValue.NewCallback((executionContext, args) =>
				UserData.Create(ToInt64(args[1], "int64")), "int64"));
			moduleTable.MetaTable = meta;
		}

		/// <summary>
		/// Coerces a script value (int64, uint64, integer-valued number, or decimal string) into a LuaInt64.
		/// </summary>
		private static LuaInt64 ToInt64(DynValue v, string funcName)
		{
			switch (v.Type)
			{
				case DataType.UserData:
					if (v.UserData != null && v.UserData.Object is LuaInt64)
						return (LuaInt64)v.UserData.Object;
					if (v.UserData != null && v.UserData.Object is LuaUInt64)
					{
						ulong u = ((LuaUInt64)v.UserData.Object).Value;
						if (u > long.MaxValue)
							throw new ScriptRuntimeException("bad argument to '{0}' (uint64 value {1} is out of range for int64)", funcName, u);
						return new LuaInt64((long)u);
					}
					throw ScriptRuntimeException.BadArgument(0, funcName, "int64, uint64, number or string expected, got userdata");
				case DataType.Number:
				{
					double d = v.Number;
					if (double.IsNaN(d) || double.IsInfinity(d) || d != System.Math.Floor(d))
						throw new ScriptRuntimeException("bad argument to '{0}' (number has no integer representation)", funcName);
					if (d < -9223372036854775808.0 || d >= 9223372036854775808.0)
						throw new ScriptRuntimeException("bad argument to '{0}' (number is out of range for int64)", funcName);
					return new LuaInt64((long)d);
				}
				case DataType.String:
				{
					long parsed;
					if (!long.TryParse(v.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
						throw new ScriptRuntimeException("bad argument to '{0}' (cannot parse '{1}' as an int64)", funcName, v.String);
					return new LuaInt64(parsed);
				}
				default:
					throw ScriptRuntimeException.BadArgument(0, funcName, "int64, uint64, number or string expected, got " + v.Type.ToLuaTypeString());
			}
		}

		[MoonSharpModuleMethod]
		public static DynValue @new(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToInt64(args[0], "int64.new"));
		}

		[MoonSharpModuleMethod]
		public static DynValue parse(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue s = args.AsType(0, "int64.parse", DataType.String, false);
			long parsed;
			if (!long.TryParse(s.String.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
				throw new ScriptRuntimeException("bad argument to 'int64.parse' (cannot parse '{0}' as an int64)", s.String);
			return UserData.Create(new LuaInt64(parsed));
		}

		[MoonSharpModuleMethod]
		public static DynValue tostring(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewString(ToInt64(args[0], "int64.tostring").ToString());
		}

		[MoonSharpModuleMethod]
		public static DynValue tonumber(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewNumber(ToInt64(args[0], "int64.tonumber").ToNumber());
		}

		[MoonSharpModuleMethod]
		public static DynValue abs(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return UserData.Create(ToInt64(args[0], "int64.abs").Abs());
		}
	}
}
