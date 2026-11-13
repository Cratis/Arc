// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_oauth_is_not_opted_into_the_pat_composition : given.an_installation
{
    void Establish()
    {
        Configure();
        _builder.AllowPersonalAccessTokenCallers();
    }

    void Because() => _builder.InstallPersonalAccessTokens(_services, []);

    [Fact] void should_not_register_the_oauth_marker() => _services.Any(descriptor => descriptor.ServiceType == typeof(OAuthCapableCallerInstallation)).ShouldBeFalse();
    [Fact] void should_not_register_an_oauth_verifier() => _services.Any(descriptor => descriptor.ServiceType == typeof(IOAuthBearerVerifier)).ShouldBeFalse();
}
