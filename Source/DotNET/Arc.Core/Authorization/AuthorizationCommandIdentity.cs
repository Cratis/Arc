// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Prevents a nested command from changing identity while it would join the outer command's transaction.
/// </summary>
internal static class AuthorizationCommandIdentity
{
    static readonly AsyncLocal<Frame?> _current = new();

    /// <summary>
    /// Enters a command frame before context providers and execution scopes run.
    /// </summary>
    /// <param name="principal">The selected or ambient identity.</param>
    /// <param name="host">The pipeline scope factory identifying one Arc host across nested executions.</param>
    /// <param name="provenance">How the caller was selected.</param>
    /// <returns>A frame restoring the parent identity after completion.</returns>
    /// <exception cref="InvalidAuthorizationConfiguration">A same-host nested command would change an active transaction's identity.</exception>
    internal static IDisposable Enter(ClaimsPrincipal? principal, object? host = null, CommandCallerProvenance provenance = CommandCallerProvenance.Ambient)
    {
        var previous = _current.Value;
        while (previous?.IsCompleted == true)
        {
            previous = previous.Previous;
        }

        ValidateNestedExecution(previous, principal, host);

        var frame = new Frame(previous, host, AuthorizationPrincipalIdentity.Capture(principal), provenance);
        _current.Value = frame;
        return frame;
    }

    /// <summary>Reads the captured caller, refusing a changed current principal.</summary>
    /// <param name="currentPrincipal">The observable current principal, if available.</param>
    /// <param name="caller">The captured immutable caller.</param>
    /// <param name="refusal">The safe refusal reason.</param>
    /// <returns>Whether the caller is available and unchanged.</returns>
    internal static bool TryGetCaller(ClaimsPrincipal? currentPrincipal, out CommandCaller? caller, out ReadModelKeyRefusal refusal)
    {
        caller = null;
        var frame = _current.Value;
        while (frame?.IsCompleted == true) frame = frame.Previous;
        refusal = ReadModelKeyRefusal.NoCaller;
        if (frame is null) return false;
        refusal = ReadModelKeyRefusal.CallerChanged;
        var callerChanged = !AuthorizationPrincipalIdentity.Same(frame.Principal, currentPrincipal);
        if (frame.CallerRefused || callerChanged) return false;
        caller = frame.Caller;
        return true;
    }

    /// <summary>Enters a non-transactional preflight frame without laundering an outer caller change.</summary>
    /// <param name="principal">The selected or ambient principal.</param>
    /// <param name="host">The Arc host identity.</param>
    /// <param name="provenance">How the caller was selected.</param>
    /// <returns>A lease restoring the previous frame.</returns>
    internal static IDisposable EnterForValidation(ClaimsPrincipal? principal, object? host, CommandCallerProvenance provenance)
    {
        var previous = _current.Value;
        while (previous?.IsCompleted == true) previous = previous.Previous;
        var sameHost = previous is not null && ReferenceEquals(previous.Host, host);
        var refused = sameHost && (previous!.CallerRefused || !AuthorizationPrincipalIdentity.Same(previous.Principal, principal));
        var frame = new Frame(previous, host, sameHost ? previous!.Principal : AuthorizationPrincipalIdentity.Capture(principal), provenance) { CallerRefused = refused, IsValidation = true };
        _current.Value = frame;
        return frame;
    }

    /// <summary>
    /// Marks that this command has an execution scope capable of owning a transaction.
    /// </summary>
    internal static void MarkTransactional()
    {
        if (_current.Value is { } current)
        {
            current.IsTransactional = true;
        }
    }

    static void ValidateNestedExecution(Frame? frame, ClaimsPrincipal? principal, object? host)
    {
        while (frame is not null)
        {
            if (IsActiveForHost(frame, host))
            {
                var changesTransaction = frame.IsTransactional && !AuthorizationPrincipalIdentity.Same(frame.Principal, principal);
                if (frame.CallerRefused || changesTransaction)
                {
                    throw new InvalidAuthorizationConfiguration("A nested command cannot change the identity of an active command transaction.");
                }
            }

            if (!frame.IsCompleted && !frame.IsValidation) return;
            frame = frame.Previous;
        }
    }

    static bool IsActiveForHost(Frame frame, object? host) => !frame.IsCompleted && ReferenceEquals(frame.Host, host);

    sealed class Frame(Frame? previous, object? host, PrincipalSnapshot principal, CommandCallerProvenance provenance) : IDisposable
    {
        public volatile bool IsCompleted;

        CommandCaller? _caller;

        public CommandCallerProvenance Provenance { get; } = provenance;

        public CommandCaller Caller => _caller ?? Interlocked.CompareExchange(ref _caller, CommandCaller.From(Principal), null) ?? _caller;

        public Frame? Previous { get; } = previous;

        public object? Host { get; } = host;

        public PrincipalSnapshot Principal { get; } = principal;

        public bool IsTransactional { get; set; }

        public bool CallerRefused { get; init; }

        public bool IsValidation { get; init; }

        public void Dispose()
        {
            IsCompleted = true;
            _current.Value = Previous;
        }
    }
}
