// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a base for an <see cref="IGuardObservableQueryEmission"/> that decides from recorded state.
/// </summary>
/// <typeparam name="TState">The type of the recorded state the decision is made from.</typeparam>
/// <remarks>
/// It re-reads the state on every guarded emission, so a subscription opened under an entitlement that has since been
/// withdrawn is terminated at its next emission instead of keeping data the caller may no longer see. It fails closed:
/// a missing key, a missing instance, a failing read or a failing decision all end the subscription, and are reported
/// through <see cref="OnDenied"/> with the same <see cref="StateDenialReason"/> a <see cref="StateAwareAuthorizationPolicy{TState}"/>
/// uses. Pair it with the policy that admitted the subscription so both decide from the same state. The guard runs once per
/// guarded emission, not per row; to react without waiting for the next emission, the host raises an emission when the state changes.
/// </remarks>
public abstract class StateAwareEmissionGuard<TState> : IGuardObservableQueryEmission
    where TState : class
{
    /// <inheritdoc/>
    public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
    {
        object? key;
        try
        {
            key = GetKey(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Deny(context, StateDenialReason.NoKey, ex);
        }

        if (key is null)
        {
            return Deny(context, StateDenialReason.NoKey, null);
        }

        TState? state;
        try
        {
            state = await Read(key, context.CancellationToken);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Deny(context, StateDenialReason.ReadFailed, ex);
        }

        if (state is null)
        {
            return Deny(context, StateDenialReason.NotFound, null);
        }

        try
        {
            return Decide(context.Principal, state)
                ? ObservableQueryEmissionVerdict.Allow
                : Deny(context, StateDenialReason.Rejected, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Deny(context, StateDenialReason.Rejected, ex);
        }
    }

    /// <summary>
    /// Gets the key of the recorded state the decision is made from, or null when the subscription names none.
    /// </summary>
    /// <param name="context">The <see cref="ObservableQueryEmissionContext"/> of the emission.</param>
    /// <returns>The key, or null.</returns>
    protected abstract object? GetKey(ObservableQueryEmissionContext context);

    /// <summary>
    /// Reads the recorded state for the key.
    /// </summary>
    /// <param name="key">The key from <see cref="GetKey"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that ends with the subscription.</param>
    /// <returns>The state, or null when none is recorded.</returns>
    protected abstract ValueTask<TState?> Read(object key, CancellationToken cancellationToken);

    /// <summary>
    /// Decides whether the caller may keep receiving data given the recorded state.
    /// </summary>
    /// <param name="principal">The caller, or null when the subscription has none.</param>
    /// <param name="state">The recorded state.</param>
    /// <returns>True to keep emitting; false to end the subscription.</returns>
    protected abstract bool Decide(ClaimsPrincipal? principal, TState state);

    /// <summary>
    /// Called when the guard ends the subscription, so the host can log why.
    /// </summary>
    /// <param name="context">The <see cref="ObservableQueryEmissionContext"/> of the emission.</param>
    /// <param name="reason">Why the subscription is ended.</param>
    /// <param name="exception">The failure, when one caused it.</param>
    protected virtual void OnDenied(ObservableQueryEmissionContext context, StateDenialReason reason, Exception? exception)
    {
    }

    ObservableQueryEmissionVerdict Deny(ObservableQueryEmissionContext context, StateDenialReason reason, Exception? exception)
    {
        OnDenied(context, reason, exception);
        return ObservableQueryEmissionVerdict.DenyAndTerminate;
    }
}
