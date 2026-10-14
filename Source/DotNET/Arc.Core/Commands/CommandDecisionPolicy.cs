// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Commands;

/// <summary>
/// The immutable protection profile of the current command invocation.
/// </summary>
public static class CommandDecisionPolicy
{
    static readonly AsyncLocal<Frame?> _current = new();
    static readonly AsyncLocal<Frame?> _queryOf = new();
    static readonly ConcurrentDictionary<Type, Profile> _profiles = new();

    /// <summary>
    /// Gets the current invocation's profile, or Legacy outside the command pipeline.
    /// </summary>
    public static CommandDecisionMode Mode => Active()?.Mode ?? CommandDecisionMode.Legacy;

    /// <summary>
    /// Gets the resolved read-protection declaration, or Off outside the command pipeline.
    /// Resolution alone does not enable reads or append guards.
    /// </summary>
    public static ReadProtectionMode ReadProtection => Active()?.ReadProtection ?? ReadProtectionMode.Off;

    /// <summary>
    /// Gets whether a command invocation is active.
    /// </summary>
    public static bool IsActive => Active() is not null;

    /// <summary>
    /// The current invocation identity, distinct for nested executions even of the same command type.
    /// </summary>
    public static object? Token => Active();

    /// <summary>
    /// Gets whether this invocation is explicitly protected.
    /// </summary>
    public static bool IsProtected => Mode == CommandDecisionMode.Protected;

    /// <summary>
    /// Gets whether discoverable validators are held to the protected-mode boundary, so only those Arc can certify run:
    /// the current invocation is protected and the caller is not a query performed from within it. A command nested in such a query starts its own invocation and is held to the boundary again.
    /// </summary>
    internal static bool RefusesDiscoverableValidators => Active() is { Mode: CommandDecisionMode.Protected } frame &&
        !ReferenceEquals(_queryOf.Value, frame);

    /// <summary>
    /// Begins an invocation before filters, validators, and dependency construction.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>A lease restoring the enclosing invocation.</returns>
    /// <remarks>
    /// A command declaring conflicting profiles runs as protected, so every protected refusal applies, and
    /// <see cref="ThrowIfConflicting"/> reports the conflict as the command's result.
    /// </remarks>
    internal static IDisposable Begin(Type commandType)
    {
        var profile = _profiles.GetOrAdd(commandType, ResolveProfile);
        var frame = new Frame(profile.Mode, profile.ReadProtection, profile.Conflict, commandType, Active());
        _current.Value = frame;
        return new Lease(frame);
    }

    /// <summary>
    /// Refuses conflicting protection declarations through the command result boundary.
    /// </summary>
    /// <exception cref="CommandCannotBeBothProtectedAndUnprotected">Conflicting protection profiles were declared.</exception>
    /// <exception cref="ConflictingReadProtection">An unprotected command explicitly enables read protection.</exception>
    internal static void ThrowIfConflicting()
    {
        if (Active() is { } frame)
        {
            CommandAttributePolicy.ThrowIfConflicting(frame.CommandType, frame.Conflict);
        }
    }

    /// <summary>
    /// Marks a query performed from within a protected invocation. The query's own validators, such as paging
    /// validation, are not part of the command's decision and are not refused.
    /// </summary>
    /// <returns>A lease restoring the previous marker, or null when no protected invocation is active.</returns>
    internal static IDisposable? BeginQuery()
    {
        if (Active() is not { Mode: CommandDecisionMode.Protected } frame)
        {
            return null;
        }

        var previous = _queryOf.Value;
        _queryOf.Value = frame;
        return new QueryLease(previous);
    }

    static Profile ResolveProfile(Type commandType)
    {
        var profile = CommandAttributePolicy.ResolveCommand(commandType);
        return new Profile((CommandDecisionMode)profile.Mode, (ReadProtectionMode)profile.ReadProtection, profile.Conflict);
    }

    static Frame? Active()
    {
        var frame = _current.Value;
        while (frame?.Completed == true)
        {
            frame = frame.Previous;
        }

        return frame;
    }

    sealed record Profile(CommandDecisionMode Mode, ReadProtectionMode ReadProtection, string? Conflict);

    sealed class Frame(CommandDecisionMode mode, ReadProtectionMode readProtection, string? conflict, Type commandType, Frame? previous)
    {
        public CommandDecisionMode Mode { get; } = mode;
        public ReadProtectionMode ReadProtection { get; } = readProtection;
        public string? Conflict { get; } = conflict;
        public Type CommandType { get; } = commandType;
        public Frame? Previous { get; } = previous;
        public bool Completed { get; set; }
    }

    sealed class Lease(Frame frame) : IDisposable
    {
        public void Dispose()
        {
            frame.Completed = true;
            _current.Value = frame.Previous;
        }
    }

    sealed class QueryLease(Frame? previous) : IDisposable
    {
        public void Dispose() => _queryOf.Value = previous;
    }
}
