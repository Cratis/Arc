// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_method_authorization;

public class with_method_with_allow_anonymous_and_type_with_authorization : given.an_authorization_helper
{
    bool _result;

    void Because() => _result = _authorizationHelper.IsAuthorized(typeof(given.TypeWithAuthorizationAndMethodAllowAnonymous).GetMethod(nameof(given.TypeWithAuthorizationAndMethodAllowAnonymous.Method))!);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
