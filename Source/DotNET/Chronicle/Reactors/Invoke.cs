// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Declares reactor command invocations.
/// </summary>
public static class Invoke
{
    /// <summary>
    /// Declares a command invocation to return from a reactor.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>An invocation to which refusal branches can be added.</returns>
    public static CommandInvocation Command(object command) => new(command, []);
}
