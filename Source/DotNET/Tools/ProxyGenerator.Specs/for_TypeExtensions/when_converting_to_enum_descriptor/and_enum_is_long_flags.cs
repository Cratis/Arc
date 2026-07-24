// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_converting_to_enum_descriptor;

public class and_enum_is_long_flags : Specification
{
    [Flags]
    enum LongFlagsEnum : long
    {
        None = 0,
        Low = 1L,
        High = 1L << 40
    }

    EnumDescriptor _result;

    void Because() => _result = typeof(LongFlagsEnum).ToEnumDescriptor();

    [Fact] void should_have_all_members() => _result.Values.Count().ShouldEqual(3);
    [Fact] void should_preserve_the_large_flag_value() => Convert.ToInt64(_result.Values.Last().Value).ShouldEqual(1L << 40);
    [Fact] void should_build_the_all_flags_expression_from_non_zero_flags() => _result.AllFlagsExpression.ShouldEqual("LongFlagsEnum.low | LongFlagsEnum.high");
}
