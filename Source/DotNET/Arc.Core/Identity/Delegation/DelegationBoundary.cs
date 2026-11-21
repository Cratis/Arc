// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation;

/// <summary>
/// Opt-in, deny-by-default delegation check. Uses the caller's current service scope without
/// creating a nested scope. A positive result is not operation authorization or approval.
/// </summary>
/// <param name="services">The current request or explicitly owned non-HTTP service scope.</param>
public sealed class DelegationBoundary(IServiceProvider services)
{
    /// <summary>
    /// Refuses malformed or non-delegated requests, missing or ambiguous authorities, and failed checks.
    /// The original verified caller and credential provenance are forwarded unchanged to exactly one authority.
    /// </summary>
    /// <param name="request">The verified delegated caller and application-selected operation context.</param>
    /// <param name="cancellationToken">Cancellation for this check, propagated rather than converted to refusal.</param>
    /// <returns>True only when one registered authority explicitly permits the request.</returns>
    public async Task<bool> IsAllowed(DelegationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsWellFormed(request)) return false;

        try
        {
            var authorities = services.GetServices<IDelegationAuthority>().ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            if (authorities.Length != 1) return false;

            var allowed = await authorities[0].IsAllowed(request, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return allowed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }
    }

    static bool IsWellFormed(DelegationRequest request) =>
        request is { Caller: { PrincipalKind: CallerPrincipalKind.DelegatedActor } caller } &&
        !string.IsNullOrWhiteSpace(request.Capability) &&
        HasMatchingDestination(request, caller) &&
        HasDelegatedPerson(caller) &&
        HasCurrentActorAndGrant(caller);

    static bool HasMatchingDestination(DelegationRequest request, VerifiedCaller caller) =>
        !string.IsNullOrWhiteSpace(request.Resource) &&
        !string.IsNullOrWhiteSpace(request.Environment) &&
        string.Equals(request.Resource, caller.Resource, StringComparison.Ordinal) &&
        string.Equals(request.Environment, caller.Environment, StringComparison.Ordinal);

    static bool HasDelegatedPerson(VerifiedCaller caller) =>
        caller.AccountKey is { } account &&
        !string.IsNullOrWhiteSpace(account.Issuer) &&
        !string.IsNullOrWhiteSpace(account.Subject) &&
        !string.IsNullOrWhiteSpace(caller.Person?.Value);

    static bool HasCurrentActorAndGrant(VerifiedCaller caller) =>
        caller.Actor is { } actor &&
        !string.IsNullOrWhiteSpace(actor.Issuer) &&
        !string.IsNullOrWhiteSpace(actor.Subject) &&
        !string.IsNullOrWhiteSpace(caller.GrantId?.Value);
}
