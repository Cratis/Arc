// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_flags_enum;

public class and_enum_does_not_have_flags_attribute : Specification
{
    enum SomeRegularEnum
    {
        First = 0,
        Second = 1,
        Third = 2
    }

    bool _result;

    void Because() => _result = typeof(SomeRegularEnum).IsFlagsEnum();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
