-- Proof / demonstration of MoonSharp's 'bigint' arbitrary-precision integer type.
--
-- Run with:
--   dotnet run --project src/MoonSharp/MoonSharp.csproj -c Release -- examples/bigint_demo.lua
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
    print(string.format("  ok   %-34s = %s", label, got))
end

local function ok(label, cond)
    assert(cond, "FAIL: " .. label)
    checks = checks + 1
    print(string.format("  ok   %s", label))
end

print("MoonSharp bigint demonstration")
print("==============================")

print("\n[construction]")
check("bigint(42)", bigint(42), "42")
check("bigint('-7')", bigint("-7"), "-7")
check("bigint(3.0)", bigint(3.0), "3")
check("bigint(<30 digits>)", bigint("123456789012345678901234567890"), "123456789012345678901234567890")

print("\n[exact arithmetic beyond 64-bit / double precision]")
check("maxint64 + 1", bigint("9223372036854775807") + 1, "9223372036854775808")
check("10^12 * 10^12", bigint("1000000000000") * bigint("1000000000000"), "1000000000000000000000000")
check("bigint.pow(2, 128)", bigint.pow(bigint(2), 128), "340282366920938463463374607431768211456")

print("\n[50! computed with bigint * (Lua number) in a loop]")
local fact = bigint(1)
for i = 1, 50 do
    fact = fact * i            -- bigint multiplied by a plain Lua number
end
check("50!", fact, "30414093201713378043612608166064768844377641568960512000000000000")

print("\n[interop with the Lua number type]")
check("bigint(10) + 5", bigint(10) + 5, "15")
check("100 - bigint(1)", 100 - bigint(1), "99")
check("bigint(7) / 2", bigint(7) / 2, "3")          -- integer division
check("bigint(7) % 2", bigint(7) % 2, "1")
check("tonumber(bigint(123)) + 1", bigint.tonumber(bigint(123)) + 1, "124")

print("\n[comparisons]")
ok("bigint(5) < bigint(10)", bigint(5) < bigint(10))
ok("bigint(10) > 5  (vs Lua number)", bigint(10) > 5)
ok("bigint(5) == bigint(5)", bigint(5) == bigint(5))
ok("bigint(5) ~= bigint(6)", bigint(5) ~= bigint(6))
ok("huge > slightly-less-huge", bigint("1000000000000000000000") > bigint("999999999999999999999"))

print(string.format("\nAll %d bigint checks passed.", checks))
