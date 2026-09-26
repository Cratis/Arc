// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class CanReadPolicy : IAuthorizationPolicy
{
    public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(context.Principal.HasClaim("permission", "read"));
}
