// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_method_authorization;

public class with_method_with_authorization_and_type_with_allow_anonymous_and_unauthenticated_user : given.an_authorization_helper
{
    bool _result;

    void Establish() => SetupUnauthenticatedUser();

    void Because() => _result = _authorizationHelper.IsAuthorized(typeof(given.TypeWithAllowAnonymousAndMethodWithAuthorization).GetMethod(nameof(given.TypeWithAllowAnonymousAndMethodWithAuthorization.Method))!);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
