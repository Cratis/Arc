// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallersBuilder;

public class when_installing_twice : given.an_installation
{
    ServiceDescriptor[] _descriptors;
    ServiceProvider _provider;
    void Establish()
    {
        Configure();
        _services.AddVerifiedCallers(_builder);
        _descriptors = _services.ToArray();
        _builder.ForResource("https://other.example", "other-audience", "other-environment");
    }
    void Because()
    {
        _exception = Catch.Exception(() => _services.AddVerifiedCallers(_builder));
        _provider = _services.BuildServiceProvider();
    }
    void Destroy() => _provider.Dispose();
    [Fact] void should_refuse_a_second_installation() => _exception.ShouldBeOfExactType<InvalidVerifiedCallerInstallation>();
    [Fact] void should_leave_the_first_installation_unchanged() => _services.SequenceEqual(_descriptors).ShouldBeTrue();
    [Fact] void should_keep_the_original_resource() => _provider.GetRequiredService<VerifiedCallerInstallation>().Resource.ShouldEqual("https://resource.example");
    [Fact] void should_keep_the_original_environment() => _provider.GetRequiredService<VerifiedCallerInstallation>().Environment.ShouldEqual("test");
    [Fact] void should_keep_the_original_audience()
    {
        _provider.GetRequiredService<CallerResourceCatalog>().TryGetAudience("https://resource.example", out var audience);
        audience.ShouldEqual("audience");
    }
}
