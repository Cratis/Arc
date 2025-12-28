// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AspNetAuthorizationAttributeEvaluator;

public class when_checking_type_without_attributes : given.an_aspnet_authorization_attribute_evaluator
{
    (bool HasAuthorize, string? Roles)? _result;

    void Because() => _result = _evaluator.GetAuthorizationInfo(typeof(given.TypeWithoutAttributes));

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
