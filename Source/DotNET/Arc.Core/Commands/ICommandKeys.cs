// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines the keys commands identify their targets by.
/// </summary>
public interface ICommandKeys
{
    /// <summary>
    /// Gets the key a command identifies its target by.
    /// </summary>
    /// <param name="command">The command to read.</param>
    /// <returns>The key, or null when no rule recognizes one on the command.</returns>
    string? GetKeyFor(object command);
}
