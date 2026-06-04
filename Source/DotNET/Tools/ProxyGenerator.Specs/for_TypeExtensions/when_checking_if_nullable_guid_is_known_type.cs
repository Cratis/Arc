// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions;

public class when_checking_if_nullable_guid_is_known_type : Specification
{
    bool _result;

    void Because() => _result = typeof(Guid?).IsKnownType();

    [Fact] void should_be_known_type() => _result.ShouldBeTrue();
}
