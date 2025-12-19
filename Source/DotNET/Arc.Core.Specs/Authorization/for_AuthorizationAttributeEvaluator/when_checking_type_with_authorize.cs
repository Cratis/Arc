// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationAttributeEvaluator;

public class when_checking_type_with_authorize : given.an_authorization_attribute_evaluator
{
    (bool HasAuthorize, string? Roles)? _result;

    void Because() => _result = _evaluator.GetAuthorizationInfo(typeof(given.TypeWithAuthorize));

    [Fact] void should_return_has_authorize_true() => _result?.HasAuthorize.ShouldBeTrue();
    [Fact] void should_return_null_roles() => _result?.Roles.ShouldBeNull();
}
