#! /usr/bin/lua
--
-- lua-TestMore : <http://fperrad.github.com/lua-TestMore/>
--

--[[

=head1 MoonSharp bigint Library

=head2 Synopsis

    % prove 315-bigint.t

=head2 Description

Tests the MoonSharp 'bigint' arbitrary-precision integer library, including
interoperability with the standard Lua numeric type.

=cut

--]]

require 'Test.More'

plan(43)

-- construction & tostring
is(tostring(bigint(42)), '42', "construct from integer")
is(tostring(bigint(-7)), '-7', "construct from negative integer")
is(tostring(bigint('123456789012345678901234567890')), '123456789012345678901234567890', "construct from string")
is(tostring(bigint(bigint(99))), '99', "construct from bigint")
is(tostring(bigint(3.0)), '3', "construct from integral Lua number")
is(tostring(bigint.new(100)), '100', "bigint.new")
is(tostring(bigint.parse('250')), '250', "bigint.parse")

-- arithmetic between two bigints
is(tostring(bigint(2) + bigint(3)), '5', "bigint + bigint")
is(tostring(bigint(9) - bigint(10)), '-1', "bigint - bigint")
is(tostring(bigint(6) * bigint(7)), '42', "bigint * bigint")
is(tostring(bigint(7) / bigint(2)), '3', "bigint / bigint (integer division)")
is(tostring(bigint(7) % bigint(2)), '1', "bigint % bigint")
is(tostring(-bigint(5)), '-5', "unary minus")

-- exact results beyond the range of Lua's number type
is(tostring(bigint('9223372036854775808') + bigint(1)), '9223372036854775809', "addition beyond 2^63")
is(tostring(bigint('1000000000000') * bigint('1000000000000')), '1000000000000000000000000', "multiplication beyond double precision")
is(tostring(bigint.pow(bigint(2), 100)), '1267650600228229401496703205376', "pow 2^100")

-- comparisons between two bigints
ok(bigint(5) < bigint(10), "bigint < bigint")
ok(bigint(10) > bigint(5), "bigint > bigint")
ok(bigint(5) <= bigint(5), "bigint <= bigint")
ok(bigint(5) >= bigint(5), "bigint >= bigint")
ok(bigint(5) == bigint(5), "bigint == bigint")
ok(bigint(5) ~= bigint(6), "bigint ~= bigint")
cmp_ok(bigint(3), '<', bigint(4), "cmp_ok with bigints")

-- interoperability with the standard Lua number type
is(tostring(bigint(10) + 5), '15', "bigint + number")
is(tostring(5 + bigint(10)), '15', "number + bigint")
is(tostring(bigint(10) - 4), '6', "bigint - number")
is(tostring(bigint(10) * 3), '30', "bigint * number")
is(tostring(bigint(10) / 3), '3', "bigint / number (integer division)")
is(tostring(bigint(10) % 3), '1', "bigint % number")
is(tostring(bigint('1000000000000') + 1), '1000000000001', "large bigint + number")
ok(bigint(5) < 10, "bigint < number")
ok(10 > bigint(5), "number > bigint")
ok(bigint(5) <= 5, "bigint <= number")
ok(7 >= bigint(5), "number >= bigint")

-- converting back to a Lua number, then using it in plain Lua arithmetic
type_ok(bigint.tonumber(bigint(123)), 'number', "tonumber yields a Lua number")
is(bigint.tonumber(bigint(123)) + 1, 124, "tonumber result used in Lua arithmetic")
is(bigint.tonumber(bigint(2) * bigint(21)), 42, "tonumber after bigint math")

-- module helpers and constants
is(tostring(bigint.abs(bigint(-17))), '17', "bigint.abs")
is(tostring(bigint.zero), '0', "bigint.zero")
is(tostring(bigint.one), '1', "bigint.one")
is(bigint.tostring(bigint(55)), '55', "bigint.tostring")

-- error handling
error_like(function () bigint('not a number') end,
           "cannot parse",
           "invalid string raises")
error_like(function () bigint(2.5) end,
           "integer representation",
           "non-integer Lua number raises")
