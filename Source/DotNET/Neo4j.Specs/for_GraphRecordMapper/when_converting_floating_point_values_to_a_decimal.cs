// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphRecordMapper;

public class when_converting_floating_point_values_to_a_decimal : Specification
{
    object _fromDouble;
    object _fromFloat;
    object _fromNegativeDouble;

    void Because()
    {
        _fromDouble = GraphRecordMapper.ConvertValue(1.23d, typeof(decimal));
        _fromFloat = GraphRecordMapper.ConvertValue(1.23f, typeof(decimal));
        _fromNegativeDouble = GraphRecordMapper.ConvertValue(-0.1000000000000005d, typeof(decimal));
    }

    [Fact] void should_convert_a_double_to_its_decimal_text() => _fromDouble.ShouldEqual(1.23m);
    [Fact] void should_convert_a_float_to_its_decimal_text() => _fromFloat.ShouldEqual(1.23m);
    [Fact] void should_round_a_double_to_15_significant_digits() => _fromNegativeDouble.ShouldEqual(-0.1m);
}
