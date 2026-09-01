// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.for_CommandScenario;

/// <summary>
/// A command whose handler takes a <see cref="TrackedResource"/> dependency, forcing the scenario's
/// service provider to materialize the registered singleton.
/// </summary>
[Command]
public record PerformWork
{
    /// <summary>
    /// Handles the command by touching the injected resource.
    /// </summary>
    /// <param name="resource">The <see cref="TrackedResource"/> resolved from the scenario's services.</param>
    public void Handle(TrackedResource resource) => resource.Touch();
}
