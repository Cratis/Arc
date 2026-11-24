// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the startup validation of availability: one pass that discovers the gated parts and verifies the declared blocks.
/// </summary>
/// <param name="catalog">The <see cref="AvailabilityCatalog"/>.</param>
/// <param name="gate">The <see cref="IAvailabilityGate"/>.</param>
[IgnoreConvention]
public class AvailabilityStartupValidation(AvailabilityCatalog catalog, IAvailabilityGate gate) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        catalog.Validate();
        _ = gate.GatedParts;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
