// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Supplies a configured scenario instant without changing production clocks.
/// </summary>
internal sealed class ScenarioTimeProvider : TimeProvider
{
    /// <summary>
    /// Gets or sets the fixed instant, or null to use the system clock.
    /// </summary>
    internal DateTimeOffset? Instant { get; set; }

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => Instant ?? System.GetUtcNow();
}
