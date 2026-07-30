// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a rule for reading the key a command identifies its target by.
/// </summary>
/// <remarks>
/// Implementations are discovered, and each is asked in turn until one recognizes a key. The rule Arc ships is asked
/// last, so an application that keys its commands its own way states that once as an implementation of this rather than
/// per command.
/// <para>
/// This is only consulted when nothing has already resolved the command's key. An integration that owns key resolution
/// — the Chronicle one, which resolves an event source id — writes the key onto the command context itself, and that
/// stands.
/// </para>
/// </remarks>
public interface ICanResolveKeyForCommand
{
    /// <summary>
    /// Reads the key from a command.
    /// </summary>
    /// <param name="command">The command to read.</param>
    /// <returns>The key, or null when this rule recognizes none on the command.</returns>
    string? Resolve(object command);
}
