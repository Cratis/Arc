// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// Renames an issue's title, quoting "the old one" &amp; the new.
/// </summary>
[Command]
public class CommandWithPunctuatedDocumentation
{
    /// <summary>
    /// Gets or sets the provider's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle()
    {
        // Nothing to do - the documentation is what is under test.
    }
}
