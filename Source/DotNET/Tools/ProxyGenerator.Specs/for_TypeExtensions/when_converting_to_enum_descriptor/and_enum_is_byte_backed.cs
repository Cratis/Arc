// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_converting_to_enum_descriptor;

public class and_enum_is_byte_backed : Specification
{
    enum ByteEnum : byte
    {
        First = 1,
        Second = 200
    }

    EnumDescriptor _result;

    void Because() => _result = typeof(ByteEnum).ToEnumDescriptor();

    [Fact] void should_have_both_members() => _result.Values.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_first_value() => Convert.ToInt64(_result.Values.First().Value).ShouldEqual(1L);
    [Fact] void should_preserve_the_second_value() => Convert.ToInt64(_result.Values.Last().Value).ShouldEqual(200L);
}
