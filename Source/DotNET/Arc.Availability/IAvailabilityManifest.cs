// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines the source of the <see cref="AvailabilityManifest"/> of the running model.
/// </summary>
public interface IAvailabilityManifest
{
    /// <summary>
    /// Gets the current manifest.
    /// </summary>
    AvailabilityManifest Current { get; }
}
