#! /usr/bin/lua
--
-- lua-TestMore : <http://fperrad.github.com/lua-TestMore/>
--

--[[

=head1 MoonSharp int64 Library

=head2 Synopsis

    % prove 316-int64.t

=head2 Description

Tests the MoonSharp 'int64' fixed-width signed 64-bit integer library, including
wraparound semantics and interoperability with the standard Lua numeric type.

=cut

--]]

require 'Test.More'

plan(32)

-- construction & tostring
is(tostring(int64(42)), '42', "construct from integer")
is(tostring(int64(-7)), '-7', "construct from negative integer")
is(tostring(int64('9223372036854775807')), '9223372036854775807', "max from string")
is(tostring(int64.new(100)), '100', "int64.new")
is(tostring(int64.parse('250')), '250', "int64.parse")
is(tostring(int64(uint64(5))), '5', "construct from uint64")

-- arithmetic between two int64 values
is(tostring(int64(2) + int64(3)), '5', "int64 + int64")
is(tostring(int64(9) - int64(10)), '-1', "int64 - int64")
is(tostring(int64(6) * int64(7)), '42', "int64 * int64")
is(tostring(int64(7) / int64(2)), '3', "int64 / int64 (integer division)")
is(tostring(int64(7) % int64(2)), '1', "int64 % int64")
is(tostring(-int64(5)), '-5', "unary minus")

-- fixed-width wraparound
is(tostring(int64('9223372036854775807') + 1), '-9223372036854775808', "max + 1 wraps to min")

-- interoperability with the standard Lua number type
is(tostring(int64(10) + 5), '15', "int64 + number")
is(tostring(5 + int64(10)), '15', "number + int64")
is(tostring(int64(10) - 4), '6', "int64 - number")
is(tostring(int64(10) * 3), '30', "int64 * number")
is(tostring(int64(10) / 3), '3', "int64 / number")
ok(int64(5) < 10, "int64 < number")
ok(10 > int64(5), "number > int64")
ok(int64(-3) < 0, "negative int64 < number")

-- comparisons between two int64 values
ok(int64(5) < int64(10), "int64 < int64")
ok(int64(5) == int64(5), "int64 == int64")
ok(int64(5) ~= int64(6), "int64 ~= int64")
cmp_ok(int64(3), '<', int64(4), "cmp_ok with int64")

-- converting back to a Lua number
type_ok(int64.tonumber(int64(123)), 'number', "tonumber yields a Lua number")
is(int64.tonumber(int64(123)) + 1, 124, "tonumber result used in Lua arithmetic")

-- helpers and constants
is(tostring(int64.abs(int64(-17))), '17', "int64.abs")
is(tostring(int64.max), '9223372036854775807', "int64.max")
is(tostring(int64.min), '-9223372036854775808', "int64.min")

-- error handling
error_like(function () int64('nope') end, "cannot parse", "invalid string raises")
error_like(function () int64(2.5) end, "integer representation", "non-integer Lua number raises")
