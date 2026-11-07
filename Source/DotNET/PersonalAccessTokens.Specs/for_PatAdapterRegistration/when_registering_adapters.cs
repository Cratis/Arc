// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.PersonalAccessTokens.for_PatAdapterRegistration;

[PerFactSpecificationLifecycle]
public class when_registering_adapters : Specification
{
    ServiceCollection _services;
    IArcBuilder _arc;

    void Establish()
    {
        _services = new();
        _arc = Substitute.For<IArcBuilder>();
        _arc.Services.Returns(_services);
    }

    [Theory]
    [InlineData(typeof(IPatAuthority))]
    [InlineData(typeof(IPatRecoveryEpoch))]
    [InlineData(typeof(IPatPolicy))]
    public void should_refuse_duplicate_adapters_on_start(Type port)
    {
        _services.AddScoped(port, _ => Substitute.For([port], []));
        Install();
        _services.AddSingleton(port, Substitute.For([port], []));
        using var provider = _services.BuildServiceProvider();
        Catch.Exception(() => provider.GetRequiredService<IStartupValidator>().Validate()).ShouldBeOfExactType<OptionsValidationException>();
    }

    [Theory]
    [InlineData(typeof(IPatAuthority))]
    [InlineData(typeof(IPatRecoveryEpoch))]
    [InlineData(typeof(IPatPolicy))]
    public void should_refuse_keyed_adapters_on_start(Type port)
    {
        Install();
        _services.AddKeyedScoped(port, "ambiguous", (_, _) => Substitute.For([port], []));
        using var provider = _services.BuildServiceProvider();
        Catch.Exception(() => provider.GetRequiredService<IStartupValidator>().Validate()).ShouldBeOfExactType<OptionsValidationException>();
    }

    [Fact]
    public void should_allow_no_adapter_at_start()
    {
        Install();
        using var provider = _services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    [Fact]
    public void should_allow_one_adapter_per_port()
    {
        Install();
        var adapter = Substitute.For<IPatAuthority, IPatRecoveryEpoch, IPatPolicy>();
        _services.AddSingleton(adapter);
        _services.AddSingleton((IPatRecoveryEpoch)adapter);
        _services.AddSingleton((IPatPolicy)adapter);
        using var provider = _services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        provider.GetRequiredService<IPatAuthority>().ShouldEqual(adapter);
    }

    [Fact]
    public void should_refuse_installing_twice()
    {
        Install();
        Catch.Exception(Install).ShouldNotBeNull();
    }

    void Install() => _arc.WithPersonalAccessTokens(new("stage-contract") { AllowedScopes = ["read"] },
        callers => callers.ForResource("resource-a", "https://resource-a.example", "test").UsePersonDirectory<for_PersonalAccessTokens.given.a_pat_module.PersonDirectory>());
}
