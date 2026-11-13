// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_oauth_callers_have_no_pat_opt_in : given.an_installation
{
    void Establish()
    {
        Configure();
        _builder.AllowOAuthBearerCallers<when_composing_oauth_and_pat_callers.Verifier>();
    }

    void Because() => _exception = Catch.Exception(() => _builder.InstallPersonalAccessTokens(_services, []));

    [Fact] void should_refuse_the_installation() => _exception.ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
    [Fact] void should_not_partially_register_services() => _services.Count.ShouldEqual(0);
}
