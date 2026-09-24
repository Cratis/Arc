// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Deliberately ignores request cancellation so the pipeline must check again before invoking the handler.
/// </summary>
/// <param name="gate">The controlled async policy gate.</param>
/// <param name="probe">The scoped policy collaborator.</param>
public class NonCooperativePolicy(PolicyGate gate, ScopedPolicyProbe probe) : IAuthorizationPolicy
{
    /// <inheritdoc/>
    public async ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
    {
        gate.Record(context.Principal.Identity?.Name, probe.Id);
        gate.ObservePolicyToken(cancellationToken);
        try
        {
            await gate.Wait(CancellationToken.None);
            return true;
        }
        finally
        {
            gate.SignalExit();
        }
    }
}
