// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

public class FixturePolicy(PolicyBehavior behavior) : IAuthorizationPolicy
{
    public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
    {
        behavior.PolicyCalls++;
        return new(behavior.Evaluate(context, cancellationToken));
    }
}
