// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedIdentityHopStartupValidation;

public class when_starting_with_required_admission : given.a_startup_validation
{
    void Establish() => RequireAdmission();

    Task Because() => _validation.StartAsync(CancellationToken.None);

    [Fact] void should_log_one_startup_warning() => Warnings.Count.ShouldEqual(1);
    [Fact] void should_explain_the_host_client_certificate_requirement() => Warnings[0].ShouldContain("Configure each Kestrel HTTPS listener with AllowCertificate or RequireCertificate");
    [Fact] void should_explain_that_arc_does_not_change_https_defaults() => Warnings[0].ShouldContain("does not change HTTPS defaults");
}
