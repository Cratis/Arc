// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_authorization;

public class with_type_with_allow_anonymous : given.an_authorization_helper
{
    bool _result;

    void Because() => _result = _authorizationHelper.IsAuthorized(typeof(given.TypeWithAllowAnonymous));

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
