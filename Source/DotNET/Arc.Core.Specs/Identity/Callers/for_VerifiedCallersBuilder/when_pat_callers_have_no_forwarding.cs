// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_pat_callers_have_no_forwarding : given.an_installation
{
    int _before;

    void Establish()
    {
        _builder.ForResource("https://resource.example", "audience", "test").UsePersonDirectory<PersonDirectory>().AllowPersonalAccessTokenCallers();
        _before = _services.Count;
    }

    void Because() => _exception = Catch.Exception(() => _builder.InstallPersonalAccessTokens(_services, []));

    [Fact] void should_refuse_incomplete_composition() => _exception.ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
    [Fact] void should_not_partially_register_services() => _services.Count.ShouldEqual(_before);
}
