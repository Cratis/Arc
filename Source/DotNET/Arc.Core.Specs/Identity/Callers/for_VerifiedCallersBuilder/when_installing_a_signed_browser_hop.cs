// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.ForwardedIdentityV2;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_installing_a_signed_browser_hop : given.an_installation
{
    ServiceProvider _provider;

    void Establish() => Configure();
    void Because()
    {
        _services.AddVerifiedCallers(_builder);
        _provider = _services.BuildServiceProvider();
    }
    void Destroy() => _provider.Dispose();

    [Fact] void should_bind_the_resource() => _provider.GetRequiredService<VerifiedCallerInstallation>().Resource.ShouldEqual("https://resource.example");
    [Fact] void should_bind_the_environment() => _provider.GetRequiredService<VerifiedCallerInstallation>().Environment.ShouldEqual("test");
    [Fact] void should_bind_one_audience() => _provider.GetRequiredService<CallerResourceCatalog>().TryGetAudience("https://resource.example", out _).ShouldBeTrue();
    [Fact] void should_require_v2() => _provider.GetRequiredService<ForwardedIdentityV2Settings>().V2Only.ShouldBeTrue();
    [Fact] void should_install_the_directory() => _provider.GetRequiredService<ICallerPersonDirectory>().ShouldBeOfExactType<PersonDirectory>();
    [Fact] void should_install_a_process_local_nonce_store() => _provider.GetRequiredService<IForwardedIdentityNonceStore>().ShouldBeOfExactType<ProcessLocalForwardedIdentityNonceStore>();
}
