// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_a_pat_capable_installation_has_no_pat_verifier : given.a_capability_startup_validation
{
    void Establish() => _services.AddSingleton(new PatCapableCallerInstallation());

    Task Because() => Validate();

    [Fact] void should_fail_startup() => _error.ShouldBeOfExactType<CapabilityHostingHasNoCredentialInstallation>();
    [Fact] void should_name_the_missing_pat_verifier() => _error!.Message.ShouldContain("PAT credential verifier");
}
