// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_the_installation_names_no_resource : given.a_capability_startup_validation
{
    void Establish()
    {
        _installation = new(null, null);
        _services.Clear();
    }

    Task Because() => Validate();

    [Fact] void should_fail_with_no_credential_installation() => _error.ShouldBeOfExactType<CapabilityHostingHasNoCredentialInstallation>();
    [Fact] void should_name_the_missing_resource_and_environment() => _error!.Message.ShouldContain("the installation names no resource and environment");
}
