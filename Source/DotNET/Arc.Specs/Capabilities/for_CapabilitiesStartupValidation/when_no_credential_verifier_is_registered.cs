// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_no_credential_verifier_is_registered : given.a_capability_startup_validation
{
    void Establish() => _services.Clear();

    Task Because() => Validate();

    [Fact] void should_fail_with_no_credential_installation() => _error.ShouldBeOfExactType<CapabilityHostingHasNoCredentialInstallation>();
    [Fact] void should_name_the_missing_verifier() => _error!.Message.ShouldContain("credential verifier");
}
