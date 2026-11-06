// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_the_catalog_maps_two_resources_to_one_audience : given.a_capability_startup_validation
{
    void Establish() => _catalog = new(new Dictionary<string, string>
    {
        ["https://resource.example"] = "audience",
        ["https://other.example"] = "audience"
    });

    Task Because() => Validate();

    [Fact] void should_fail_with_no_credential_installation() => _error.ShouldBeOfExactType<CapabilityHostingHasNoCredentialInstallation>();
}
