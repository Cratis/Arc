// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_authorization;

public class with_type_with_multiple_roles_and_user_has_no_required_roles : given.an_authorization_helper
{
    bool _result;

    void Establish() => SetupAuthenticatedUser("Guest");

    void Because() => _result = _authorizationHelper.IsAuthorized(typeof(given.TypeWithMultipleRoles));

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}