// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.when_authorizing_under_system_execution;

public class and_the_system_actor_has_the_required_role : given.composed_authorization
{
    bool _result;

    void Because()
    {
        using (_systemExecution.AsSystem("Admin"))
        {
            _result = _authorizationEvaluator.IsAuthorized(typeof(RoleGatedType));
        }
    }

    [Fact] void should_be_authorized() => _result.ShouldBeTrue();
}
