// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_composing_pat_and_signed_callers : given.an_installation
{
    CallerCredentialTarget _target;

    void Establish()
    {
        Configure();
        _builder.AllowPersonalAccessTokenCallers();
    }

    void Because() => _target = _builder.InstallPersonalAccessTokens(_services, ["retired_"]);

    [Fact] void should_keep_the_exact_resource() => _target.Resource.ShouldEqual("https://resource.example");
    [Fact] void should_register_one_installation() => _services.Count(descriptor => descriptor.ServiceType == typeof(VerifiedCallerInstallation)).ShouldEqual(1);
    [Fact] void should_register_one_catalog() => _services.Count(descriptor => descriptor.ServiceType == typeof(CallerResourceCatalog)).ShouldEqual(1);
    [Fact] void should_register_one_directory() => _services.Count(descriptor => descriptor.ServiceType == typeof(ICallerPersonDirectory)).ShouldEqual(1);
    [Fact] void should_keep_the_signed_source() => _services.Count(descriptor => descriptor.ServiceType == typeof(IVerifiedCredentialSource)).ShouldEqual(1);
    [Fact] void should_mark_pat_capability() => _services.Count(descriptor => descriptor.ServiceType == typeof(PatCapableCallerInstallation)).ShouldEqual(1);
    [Fact] void should_reject_another_installation() => Catch.Exception(() => _builder.Install(_services)).ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
}
