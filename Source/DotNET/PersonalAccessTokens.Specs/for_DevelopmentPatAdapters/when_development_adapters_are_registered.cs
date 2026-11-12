// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens.Development;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.PersonalAccessTokens.for_DevelopmentPatAdapters;

public class when_development_adapters_are_registered : Specification
{
    ServiceProvider _before;
    ServiceProvider _after;
    IHostEnvironment _environment;
    Exception? _startupError;
    string? _beforeError;
    string? _afterError;

    void Establish()
    {
        _environment = Substitute.For<IHostEnvironment>();
        _environment.EnvironmentName.Returns(Environments.Development);
        _before = Install(true);
        _after = Install(false);
    }

    async Task Because()
    {
        var validator = _before.GetServices<IHostedService>().OfType<DevelopmentPatStartupValidation>().Single();
        await validator.StartAsync(TestContext.Current.CancellationToken);
        _environment.EnvironmentName.Returns(Environments.Production);
        _startupError = Catch.Exception(() => validator.StartAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult());
    }

    [Fact] void should_register_authority_before_pat_installation() => _before.GetRequiredService<IPatAuthority>().ShouldBeOfExactType<DevelopmentPatAuthority>();
    [Fact] void should_register_policy_after_pat_installation() => _after.GetRequiredService<IPatPolicy>().ShouldBeOfExactType<DevelopmentPatPolicy>();
    [Fact] void should_register_recovery_evidence() => _after.GetRequiredService<IPatRecoveryEpoch>().ShouldBeOfExactType<DevelopmentPatRecoveryEpoch>();
    [Fact] void should_have_unambiguous_registration_before_installation() => _beforeError.ShouldBeNull();
    [Fact] void should_have_unambiguous_registration_after_installation() => _afterError.ShouldBeNull();
    [Fact] void should_refuse_startup_outside_development() => _startupError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_explain_the_required_environment() => _startupError!.Message.ShouldContain("Development host environment");

    ServiceProvider Install(bool adaptersFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_environment);
        var builder = Substitute.For<IArcBuilder>();
        builder.Services.Returns(services);
        if (adaptersFirst) builder.UseDevelopmentPersonalAccessTokenAdapters();
        builder.WithPersonalAccessTokens(new("direct") { AllowedScopes = ["read"] }, callers => callers
            .ForResource("resource", "audience", "Development")
            .UsePersonDirectory<for_PersonalAccessTokens.given.a_pat_module.PersonDirectory>());
        if (!adaptersFirst) builder.UseDevelopmentPersonalAccessTokenAdapters();
        var error = PatAdapterRegistrationCheck.Validate(services);
        if (adaptersFirst) _beforeError = error;
        else _afterError = error;
        return services.BuildServiceProvider();
    }

    void Destroy()
    {
        _before.Dispose();
        _after.Dispose();
    }
}
