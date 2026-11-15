// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents a set of values associated with a <see cref="CommandContext"/>.
/// </summary>
public class CommandContextValues : Dictionary<string, object>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContextValues"/> class.
    /// </summary>
    public CommandContextValues()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Gets a new, empty set of command context values.
    /// </summary>
    /// <remarks>
    /// Each access returns a new instance. Command context values are writable, so a shared instance
    /// would leak values written by one command context into every other context that uses it.
    /// </remarks>
    public static CommandContextValues Empty => new();

    /// <summary>
    /// Merges another set of command context values into this instance.
    /// </summary>
    /// <param name="other">The other set of command context values to merge.</param>
    public void Merge(CommandContextValues other)
    {
        foreach (var kvp in other)
        {
            this[kvp.Key] = kvp.Value;
        }
    }
}
