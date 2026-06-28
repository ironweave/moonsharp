-- Proof / demonstration of MoonSharp's fixed-width integer types 'int64' and 'uint64'.
--
-- Run with:
--   dotnet run --project src/MoonSharp/MoonSharp.csproj -c Release -- examples/int64_uint64_demo.lua
--
-- Every step asserts its expected result, so any failure raises an error and a
-- non-zero exit. Reaching the final line proves the functionality works.

local checks = 0

local function check(label, got, expected)
    got = tostring(got)
    expected = tostring(expected)
    assert(got == expected,
        string.format("FAIL: %s -> got '%s', expected '%s'", label, got, expected))
    checks = checks + 1
    print(string.format("  ok   %-36s = %s", label, got))
end

local function ok(label, cond)
    assert(cond, "FAIL: " .. label)
    checks = checks + 1
    print(string.format("  ok   %s", label))
end

print("MoonSharp int64 / uint64 demonstration")
print("======================================")

print("\n[int64 - signed, wraps like .NET long]")
check("int64(42)", int64(42), "42")
check("int64('-7')", int64("-7"), "-7")
check("int64.max", int64.max, "9223372036854775807")
check("int64.min", int64.min, "-9223372036854775808")
check("int64.max + 1 (wraps)", int64.max + 1, "-9223372036854775808")
check("int64(10) + 5  (Lua number)", int64(10) + 5, "15")
check("int64(7) / 2   (integer div)", int64(7) / 2, "3")
check("int64.abs(int64(-17))", int64.abs(int64(-17)), "17")
ok("int64(-3) < 0", int64(-3) < 0)

print("\n[uint64 - unsigned, wraps like .NET ulong]")
check("uint64(42)", uint64(42), "42")
check("uint64.max", uint64.max, "18446744073709551615")
check("uint64(0) - 1 (wraps to max)", uint64(0) - 1, "18446744073709551615")
check("uint64.max + 1 (wraps to 0)", uint64.max + 1, "0")
check("uint64(10) * 3  (Lua number)", uint64(10) * 3, "30")
ok("uint64(0) > -1 (never negative)", uint64(0) > -1)
ok("huge ordering", uint64("18446744073709551615") > uint64("18446744073709551614"))

print("\n[conversions between the two]")
check("int64(uint64(5))", int64(uint64(5)), "5")
check("uint64(int64(5))", uint64(int64(5)), "5")
check("tonumber(int64(123)) + 1", int64.tonumber(int64(123)) + 1, "124")

print(string.format("\nAll %d int64/uint64 checks passed.", checks))
