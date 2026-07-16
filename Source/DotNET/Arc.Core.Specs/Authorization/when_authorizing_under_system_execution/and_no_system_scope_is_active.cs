// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.when_authorizing_under_system_execution;

public class and_no_system_scope_is_active : given.composed_authorization
{
    bool _result;

    void Because() => _result = _authorizationEvaluator.IsAuthorized(typeof(RoleGatedType));

    [Fact] void should_not_be_authorized() => _result.ShouldBeFalse();
}
