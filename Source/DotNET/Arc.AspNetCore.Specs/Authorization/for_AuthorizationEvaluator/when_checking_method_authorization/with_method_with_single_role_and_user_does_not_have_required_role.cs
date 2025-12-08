// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Reflection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_method_authorization;

public class with_method_with_single_role_and_user_does_not_have_required_role : given.an_authorization_helper
{
    bool _result;
    MethodInfo _method;

    void Establish()
    {
        _method = typeof(given.TypeWithMethodSingleRole).GetMethod(nameof(given.TypeWithMethodSingleRole.MethodWithSingleRole))!;
        SetupAuthenticatedUser("User");
    }

    void Because() => _result = _authorizationHelper.IsAuthorized(_method);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}