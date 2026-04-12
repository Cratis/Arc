// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace TestApps.Features.CrossCuttingAuthorization;

/// <summary>
/// Represents a command used to demonstrate namespace-based cross-cutting authorization filters.
/// </summary>
/// <param name="Message">The message to echo back from the command execution.</param>
[Command]
public record RunSecuredCommand(string Message)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <returns>A message indicating when the command was authorized and executed.</returns>
    public string Handle() =>
        $"Command authorized and executed at {DateTimeOffset.UtcNow:HH:mm:ss} — {Message}";
}
