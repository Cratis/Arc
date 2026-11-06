// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_the_resource_has_no_audience : given.a_capability_startup_validation
{
    void Establish()
    {
        _catalog = new(new Dictionary<string, string>());
        _services.Clear();
    }

    Task Because() => Validate();

    [Fact] void should_fail_with_no_credential_installation() => _error.ShouldBeOfExactType<CapabilityHostingHasNoCredentialInstallation>();
    [Fact] void should_name_the_missing_audience() => _error!.Message.ShouldContain("the resource has no single receiving audience in the resource catalog");
}
