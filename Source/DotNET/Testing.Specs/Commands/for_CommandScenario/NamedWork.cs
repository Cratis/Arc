// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.for_CommandScenario;

/// <summary>
/// A command with a discovered validator, used to prove which service provider a scenario's validators resolve from.
/// </summary>
/// <param name="Name">The name to validate.</param>
[Command]
public record NamedWork(string Name)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle()
    {
    }
}
