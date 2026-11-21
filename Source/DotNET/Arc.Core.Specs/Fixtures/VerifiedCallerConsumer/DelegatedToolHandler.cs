// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.Delegation;

namespace VerifiedCallerConsumer;

/// <summary>
/// Compiles the opt-in delegation path using only public APIs. The operation callback represents
/// separate product membership and operation authorization, not an authority granted by this handler.
/// </summary>
/// <param name="callers">The host's scoped verified caller accessor.</param>
/// <param name="delegation">The host's explicitly registered scoped delegation boundary.</param>
public sealed class DelegatedToolHandler(IVerifiedCallerAccessor callers, DelegationBoundary delegation)
{
    /// <summary>
    /// Resolves a verified caller, checks delegation, then reaches separate product authorization.
    /// No request-supplied grant, actor, budget, or destination is accepted.
    /// </summary>
    /// <param name="authorizeAndExecute">The separate product authorization and operation path.</param>
    /// <param name="cancellationToken">Cancellation for the delegation check.</param>
    /// <returns>The product result, or null without reaching the product path on refusal.</returns>
    public async Task<string?> TryRead(Func<VerifiedCaller, string> authorizeAndExecute, CancellationToken cancellationToken = default)
    {
        var resolution = await callers.GetCurrent().ConfigureAwait(false);
        if (resolution.Caller is not { } caller) return null;

        var request = new DelegationRequest(caller, "fixture:read", "https://api.example.test", "test");
        if (!await delegation.IsAllowed(request, cancellationToken).ConfigureAwait(false)) return null;

        return authorizeAndExecute(caller);
    }
}
