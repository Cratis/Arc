// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_well_known_type;

public class with_regular_type : Specification
{
    bool _result;

    void Because() => _result = typeof(string).IsWellKnownType();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
