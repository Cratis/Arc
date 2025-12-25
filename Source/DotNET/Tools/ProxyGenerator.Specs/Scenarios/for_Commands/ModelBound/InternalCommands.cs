// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

/// <summary>
/// An internal command for testing internal type support.
/// </summary>
[Command]
internal class InternalCommand
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command internally.
    /// </summary>
    internal void Handle()
    {
        // Internal command logic
    }
}

/// <summary>
/// A public command with internal Handle method for testing.
/// </summary>
[Command]
public class CommandWithInternalHandle
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Internal handle method.
    /// </summary>
    internal void Handle()
    {
        // Internal handle logic
    }
}
