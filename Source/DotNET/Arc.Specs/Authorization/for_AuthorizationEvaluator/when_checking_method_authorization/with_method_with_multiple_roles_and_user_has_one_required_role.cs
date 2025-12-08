// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Reflection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.when_checking_method_authorization;

public class with_method_with_multiple_roles_and_user_has_one_required_role : given.an_authorization_helper
{
    bool _result;
    MethodInfo _method;

    void Establish()
    {
        _method = typeof(given.TypeWithMethodMultipleRoles).GetMethod(nameof(given.TypeWithMethodMultipleRoles.MethodWithMultipleRoles))!;
        SetupAuthenticatedUser("Admin");
    }

    void Because() => _result = _authorizationHelper.IsAuthorized(_method);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}