#! /usr/bin/lua
--
-- lua-TestMore : <http://fperrad.github.com/lua-TestMore/>
--

--[[

=head1 MoonSharp uint64 Library

=head2 Synopsis

    % prove 317-uint64.t

=head2 Description

Tests the MoonSharp 'uint64' fixed-width unsigned 64-bit integer library, including
wraparound semantics and interoperability with the standard Lua numeric type.

=cut

--]]

require 'Test.More'

plan(27)

-- construction & tostring
is(tostring(uint64(42)), '42', "construct from integer")
is(tostring(uint64('18446744073709551615')), '18446744073709551615', "max from string")
is(tostring(uint64.new(100)), '100', "uint64.new")
is(tostring(uint64.parse('250')), '250', "uint64.parse")
is(tostring(uint64(int64(5))), '5', "construct from int64")

-- arithmetic between two uint64 values
is(tostring(uint64(2) + uint64(3)), '5', "uint64 + uint64")
is(tostring(uint64(6) * uint64(7)), '42', "uint64 * uint64")
is(tostring(uint64(7) / uint64(2)), '3', "uint64 / uint64 (integer division)")
is(tostring(uint64(7) % uint64(2)), '1', "uint64 % uint64")

-- fixed-width wraparound
is(tostring(uint64(0) - 1), '18446744073709551615', "0 - 1 wraps to max")
is(tostring(uint64('18446744073709551615') + 1), '0', "max + 1 wraps to 0")

-- interoperability with the standard Lua number type
is(tostring(uint64(10) + 5), '15', "uint64 + number")
is(tostring(5 + uint64(10)), '15', "number + uint64")
is(tostring(uint64(10) * 3), '30', "uint64 * number")
ok(uint64(5) < 10, "uint64 < number")
ok(uint64(0) > -1, "uint64 is never negative")

-- comparisons between two uint64 values
ok(uint64(5) < uint64(10), "uint64 < uint64")
ok(uint64(5) == uint64(5), "uint64 == uint64")
ok(uint64(5) ~= uint64(6), "uint64 ~= uint64")
ok(uint64('18446744073709551615') > uint64('18446744073709551614'), "ordering near the top of the range")

-- converting back to a Lua number
type_ok(uint64.tonumber(uint64(123)), 'number', "tonumber yields a Lua number")
is(uint64.tonumber(uint64(123)) + 1, 124, "tonumber result used in Lua arithmetic")

-- constants
is(tostring(uint64.max), '18446744073709551615', "uint64.max")
is(tostring(uint64.min), '0', "uint64.min")

-- error handling
error_like(function () uint64(-1) end, "out of range", "negative number raises")
error_like(function () uint64(int64(-1)) end, "negative", "negative int64 raises")
error_like(function () uint64('nope') end, "cannot parse", "invalid string raises")
