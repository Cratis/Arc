// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

/// <summary>
/// Adds the scoped probe controller to one host only, so other hosts in the spec assembly never discover it.
/// </summary>
public class ScopedProbeControllerFeature : IApplicationFeatureProvider<ControllerFeature>
{
    /// <inheritdoc/>
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) =>
        feature.Controllers.Add(typeof(ScopedProbeController).GetTypeInfo());
}
