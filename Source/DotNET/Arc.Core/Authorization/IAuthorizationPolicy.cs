// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// A host-neutral, named asynchronous authorization policy.
/// </summary>
public interface IAuthorizationPolicy
{
    /// <summary>
    /// Evaluates access to one command or query.
    /// </summary>
    /// <param name="context">The caller and resource.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    /// <returns>True if access is granted.</returns>
    ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken);
}
