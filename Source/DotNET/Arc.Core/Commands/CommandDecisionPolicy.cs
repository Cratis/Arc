// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>The immutable protection profile of the current command invocation.</summary>
public static class CommandDecisionPolicy
{
    static readonly AsyncLocal<Frame?> _current = new();

    /// <summary>Gets the current invocation's profile, or Legacy outside the command pipeline.</summary>
    public static CommandDecisionMode Mode => Active()?.Mode ?? CommandDecisionMode.Legacy;

    /// <summary>Gets whether a command invocation is active.</summary>
    public static bool IsActive => Active() is not null;

    /// <summary>The current invocation identity, distinct for nested executions even of the same command type.</summary>
    public static object? Token => Active();

    /// <summary>Gets whether this invocation is explicitly protected.</summary>
    public static bool IsProtected => Mode == CommandDecisionMode.Protected;

    /// <summary>Begins an invocation before filters, validators, and dependency construction.</summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>A lease restoring the enclosing invocation.</returns>
    /// <exception cref="InvalidOperationException">Conflicting protection profiles were declared.</exception>
    internal static IDisposable Begin(Type commandType)
    {
        var attributes = commandType.GetCustomAttributes(true);
        var isProtected = attributes.Any(_ => _ is IProtectedDecisionCommand);
        var isUnprotected = attributes.Any(_ => _ is IUnprotectedDecisionCommand);
        if (isProtected && isUnprotected)
        {
            throw new InvalidOperationException($"Command '{commandType}' cannot be both protected and unprotected.");
        }

        var frame = new Frame(isProtected ? CommandDecisionMode.Protected : isUnprotected ? CommandDecisionMode.Unprotected : CommandDecisionMode.Legacy, Active());
        _current.Value = frame;
        return new Lease(frame);
    }

    static Frame? Active()
    {
        var frame = _current.Value;
        while (frame?.Completed == true) frame = frame.Previous;
        return frame;
    }

    sealed class Frame(CommandDecisionMode mode, Frame? previous)
    {
        public CommandDecisionMode Mode { get; } = mode;
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
}
