// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_flags_enum;

public class and_enum_has_flags_attribute : Specification
{
    [Flags]
    enum SomeFlagsEnum
    {
        None = 0,
        First = 1 << 0,
        Second = 1 << 1,
        Both = First | Second
    }

    bool _result;

    void Because() => _result = typeof(SomeFlagsEnum).IsFlagsEnum();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
