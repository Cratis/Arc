// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_composing_oauth_and_pat_callers : given.an_installation
{
    void Establish()
    {
        Configure();
        _builder.AllowPersonalAccessTokenCallers().AllowOAuthBearerCallers<Verifier>();
    }

    void Because() => _builder.InstallPersonalAccessTokens(_services, []);

    [Fact] void should_register_the_oauth_marker() => _services.Count(descriptor => descriptor.ServiceType == typeof(OAuthCapableCallerInstallation)).ShouldEqual(1);
    [Fact] void should_register_a_scoped_public_verifier() => _services.Single(descriptor => descriptor.ServiceType == typeof(IOAuthBearerVerifier)).Lifetime.ShouldEqual(ServiceLifetime.Scoped);
    [Fact] void should_not_register_a_raw_oauth_source() => _services.Count(descriptor => descriptor.ServiceType == typeof(IVerifiedCredentialSource)).ShouldEqual(1);
    [Fact] void should_reject_a_second_installation() => Catch.Exception(() => _builder.InstallPersonalAccessTokens(_services, [])).ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();

    public class Verifier : IOAuthBearerVerifier
    {
        public Task<OAuthBearerVerification> Verify(OAuthBearerPresentation presentation, CancellationToken cancellationToken) => Task.FromResult(OAuthBearerVerification.Refused());
    }
}
