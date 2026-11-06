// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityHopStartupValidation;

public class when_starting_without_required_admission : given.a_startup_validation
{
    void Establish() => _options.ForwardedIdentity.ClientCertificate.TrustedCertificateAuthoritiesPath = "unused-invalid-bundle";

    Task Because() => _validation.StartAsync(CancellationToken.None);

    [Fact] void should_not_log_a_startup_warning() => Warnings.ShouldBeEmpty();
}
