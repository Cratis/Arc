// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Declares reactor command invocations and acknowledgment branches.
/// </summary>
public static class Invoke
{
    /// <summary>
    /// Declares a command invocation to return from a reactor.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>An invocation to which refusal branches can be added.</returns>
    public static CommandInvocation Command(object command) => new(command, []);

    /// <summary>
    /// Acknowledges a refusal without producing an event. This does not prove the command previously completed.
    /// </summary>
    /// <param name="refusal">The refused command's details.</param>
    /// <returns>Null, representing acknowledgment.</returns>
    public static object? Acknowledge(CommandRefusal refusal)
    {
        _ = refusal;

        return null;
    }
}
