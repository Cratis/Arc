// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Base class for a policy that decides from state the application has recorded, such as the plan of the asset a command acts on.
/// </summary>
/// <remarks>
/// The policy fails closed: a missing key, absent state, a failed read or a rejecting decision all deny.
/// The cause of a denial is reported through <see cref="OnDenied"/> so it stays distinguishable from an
/// exception. The read is an admission check; it does not enrol in decision reads and nothing revokes access
/// that was granted when the state later changes. Cancellation is not a denial and propagates.
/// </remarks>
/// <typeparam name="TState">The recorded state the decision is taken from.</typeparam>
public abstract class StateAwareAuthorizationPolicy<TState> : IAuthorizationPolicy
    where TState : class
{
    /// <inheritdoc/>
    public async ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
    {
        object? key;
        try
        {
            key = GetKey(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnDenied(context, StateDenialReason.NoKey, ex);
            return false;
        }

        if (key is null)
        {
            OnDenied(context, StateDenialReason.NoKey, null);
            return false;
        }

        TState? state;
        try
        {
            state = await Read(key, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            OnDenied(context, StateDenialReason.ReadFailed, ex);
            return false;
        }

        if (state is null)
        {
            OnDenied(context, StateDenialReason.NotFound, null);
            return false;
        }

        bool allowed;
        try
        {
            allowed = Decide(context.Principal, state);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnDenied(context, StateDenialReason.Rejected, ex);
            return false;
        }

        if (!allowed)
        {
            OnDenied(context, StateDenialReason.Rejected, null);
        }

        return allowed;
    }

    /// <summary>
    /// Takes the key of the state from the command or query being authorized. Return <see langword="null"/> when there is none.
    /// </summary>
    /// <param name="context">The caller and resource.</param>
    /// <returns>The key, or <see langword="null"/> to deny.</returns>
    protected abstract object? GetKey(AuthorizationPolicyContext context);

    /// <summary>
    /// Reads the recorded state, typically a read model, by key. Return <see langword="null"/> when none exists.
    /// </summary>
    /// <param name="key">The key from <see cref="GetKey"/>.</param>
    /// <param name="cancellationToken">The execution cancellation token.</param>
    /// <returns>The state, or <see langword="null"/> when absent.</returns>
    protected abstract ValueTask<TState?> Read(object key, CancellationToken cancellationToken);

    /// <summary>
    /// Decides from the recorded state and the caller.
    /// </summary>
    /// <param name="principal">The caller.</param>
    /// <param name="state">The recorded state.</param>
    /// <returns>True when access is granted.</returns>
    protected abstract bool Decide(ClaimsPrincipal principal, TState state);

    /// <summary>
    /// Called when access is denied, so the cause can be logged or counted. The default does nothing.
    /// </summary>
    /// <param name="context">The caller and resource.</param>
    /// <param name="reason">Why access was denied.</param>
    /// <param name="exception">The failure behind the denial, when there was one.</param>
    protected virtual void OnDenied(AuthorizationPolicyContext context, StateDenialReason reason, Exception? exception)
    {
    }
}
