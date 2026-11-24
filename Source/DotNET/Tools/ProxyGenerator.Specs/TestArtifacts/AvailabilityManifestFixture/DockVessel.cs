// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Specs.AvailabilityManifestFixture;

[Command]
[AvailableFor("Plus", "Preview")]
[AvailableWith("Paid")]
public class DockVessel
{
    public string VesselId { get; set; } = string.Empty;

    public void Handle()
    {
    }
}
