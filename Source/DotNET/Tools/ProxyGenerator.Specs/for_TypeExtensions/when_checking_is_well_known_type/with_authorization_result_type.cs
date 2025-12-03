// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_checking_is_well_known_type;

public class with_authorization_result_type : Specification
{
    bool _result;

    void Because() => _result = typeof(AuthorizationResult).IsWellKnownType();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
