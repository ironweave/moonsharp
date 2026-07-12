#! /usr/bin/lua
--
-- lua-TestMore : <http://fperrad.github.com/lua-TestMore/>
--

--[[

=head1 MoonSharp uint256 Library

=head2 Synopsis

    % prove 318-uint256.t

=head2 Description

Tests the MoonSharp 'uint256' fixed-width unsigned 256-bit integer library, including
checked (trapping) arithmetic, integer square root, hex codec, and interoperability with
the sibling numeric types and the standard Lua number type.

=cut

--]]

require 'Test.More'

plan(45)

local MAX = '115792089237316195423570985008687907853269984665640564039457584007913129639935' -- 2^256 - 1

-- construction & tostring
is(tostring(uint256(42)), '42', "construct from integer")
is(tostring(uint256(MAX)), MAX, "max from string")
is(tostring(uint256.new(100)), '100', "uint256.new")
is(tostring(uint256.parse('250')), '250', "uint256.parse")
is(tostring(uint256(uint64(5))), '5', "construct from uint64")
is(tostring(uint256(int64(7))), '7', "construct from int64")
is(tostring(uint256(bigint(9))), '9', "construct from bigint")

-- arithmetic between two uint256 values
is(tostring(uint256(2) + uint256(3)), '5', "uint256 + uint256")
is(tostring(uint256(6) * uint256(7)), '42', "uint256 * uint256")
is(tostring(uint256(10) - uint256(4)), '6', "uint256 - uint256")
is(tostring(uint256(7) / uint256(2)), '3', "uint256 / uint256 (integer division)")
is(tostring(uint256(7) % uint256(2)), '1', "uint256 % uint256")

-- wide arithmetic that overflows uint64 but fits uint256
is(tostring(uint256('18446744073709551616') * uint256('18446744073709551616')),
   '340282366920938463463374607431768211456', "2^64 * 2^64 = 2^128")
is(tostring(uint256(MAX) - uint256(MAX) + uint256(1)), '1', "arithmetic at the top of the range")

-- checked arithmetic: overflow/underflow trap instead of wrapping
error_like(function () return uint256(MAX) + uint256(1) end, "overflow", "add past 2^256 traps")
error_like(function () return uint256(0) - uint256(1) end, "underflow", "subtract below zero traps")
error_like(function () return uint256(MAX) * uint256(2) end, "overflow", "multiply past 2^256 traps")
error_like(function () return uint256(1) / uint256(0) end, "division by zero", "divide by zero traps")
error_like(function () return uint256(1) % uint256(0) end, "modulo by zero", "modulo by zero traps")

-- integer square root (exact, at boundaries)
is(tostring(uint256.isqrt(uint256(0))), '0', "isqrt(0)")
is(tostring(uint256.isqrt(uint256(1))), '1', "isqrt(1)")
is(tostring(uint256.isqrt(uint256(15))), '3', "isqrt(15) = 3 (floor)")
is(tostring(uint256.isqrt(uint256(16))), '4', "isqrt(16) = 4 (perfect square)")
is(tostring(uint256.isqrt(uint256(17))), '4', "isqrt(17) = 4 (floor)")
is(tostring(uint256.isqrt(uint256('340282366920938463463374607431768211456'))),
   '18446744073709551616', "isqrt(2^128) = 2^64")
is(tostring(uint256.isqrt(uint256(MAX))), '340282366920938463463374607431768211455',
   "isqrt(2^256 - 1) = 2^128 - 1")

-- hex codec
is(uint256.tohex(uint256(0)), string.rep('0', 64), "tohex(0) is 64 zeros")
is(uint256.tohex(uint256(255)), string.rep('0', 62) .. 'ff', "tohex(255)")
is(tostring(uint256.fromhex('ff')), '255', "fromhex('ff')")
is(tostring(uint256.fromhex('0xFF')), '255', "fromhex with 0x prefix and uppercase")
is(uint256.tohex(uint256.fromhex(string.rep('f', 64))), string.rep('f', 64), "hex round-trip at max")
error_like(function () return uint256.fromhex('xyz') end, "invalid digit", "non-hex traps")
error_like(function () return uint256.fromhex(string.rep('f', 65)) end, "64 hex digits", "over-length hex traps")

-- interoperability with the standard Lua number type
is(tostring(uint256(10) + 5), '15', "uint256 + number")
is(tostring(5 + uint256(10)), '15', "number + uint256")
ok(uint256(5) < 10, "uint256 < number")
ok(uint256(0) > -1, "uint256 is never negative")

-- cross-type value equality and ordering (NumericInterop)
ok(uint256(5) == 5, "uint256 == number")
ok(uint256(5) == uint64(5), "uint256 == uint64")
ok(uint256(5) == bigint(5), "uint256 == bigint")
ok(uint256(6) >= uint64(5), "uint256 >= uint64")

-- narrowing back to uint64 (trapping when out of range)
is(tostring(uint64(uint256(123))), '123', "uint64(uint256) narrows in range")
error_like(function () return uint64(uint256(MAX)) end, "does not fit", "narrowing an out-of-range uint256 traps")

-- constants
is(tostring(uint256.max), MAX, "uint256.max")
is(tostring(uint256.min), '0', "uint256.min")

-- error handling
error_like(function () return uint256(-1) end, "out of range", "negative number raises")
error_like(function () return uint256('nope') end, "cannot parse", "invalid string raises")
