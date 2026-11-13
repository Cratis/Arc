// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_conventions_run_without_the_oauth_composition : given.an_installation
{
    OAuthCapableCallerInstallation _marker;

    void Establish()
    {
        Configure();
        _builder.AllowPersonalAccessTokenCallers();
        _builder.InstallPersonalAccessTokens(_services, []);
    }

    void Because()
    {
        _services.AddArcServiceBindings();
        using var provider = _services.BuildServiceProvider();
        _marker = provider.GetService<OAuthCapableCallerInstallation>();
    }

    [Fact] void should_not_resolve_the_oauth_composition_marker() => _marker.ShouldBeNull();
}
