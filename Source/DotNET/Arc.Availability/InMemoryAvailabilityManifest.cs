// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// An <see cref="IAvailabilityManifest"/> that holds a manifest given to it.
/// </summary>
/// <param name="current">The manifest.</param>
[IgnoreConvention]
public class InMemoryAvailabilityManifest(AvailabilityManifest current) : IAvailabilityManifest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryAvailabilityManifest"/> class that declares nothing.
    /// </summary>
    public InMemoryAvailabilityManifest()
        : this(AvailabilityManifest.Empty)
    {
    }

    /// <inheritdoc/>
    public AvailabilityManifest Current => current;
}
