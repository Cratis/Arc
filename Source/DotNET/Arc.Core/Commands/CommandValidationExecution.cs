// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>Identifies a validation-only command invocation to optional read providers.</summary>
public static class CommandValidationExecution
{
    static readonly AsyncLocal<(object Token, Type CommandType)?> _active = new();

    /// <summary>Gets whether this async flow is validating without executing the command.</summary>
    public static bool IsActive => _active.Value is not null;

    /// <summary>Gets the identity and command type of the current validation invocation, if any.</summary>
    public static (object Token, Type CommandType)? Current => _active.Value;

    /// <summary>Begins the validation-only flow, restoring any previous invocation on disposal.</summary>
    /// <param name="commandType">The command type being validated.</param>
    /// <returns>A lease for the validation invocation.</returns>
    internal static IDisposable Begin(Type commandType)
    {
        var previous = _active.Value;
        _active.Value = (new object(), commandType);
        return new Lease(previous);
    }

    /// <summary>Suspends an enclosing validation-only flow while a nested command actually executes.</summary>
    /// <returns>A lease that restores the enclosing validation flow.</returns>
    internal static IDisposable Suspend()
    {
        var previous = _active.Value;
        _active.Value = null;
        return new Lease(previous);
    }

    sealed class Lease((object Token, Type CommandType)? previous) : IDisposable
    {
        public void Dispose() => _active.Value = previous;
    }
}
