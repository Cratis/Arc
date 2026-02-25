// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcApplicationBuilderExtensions;

[Collection("UsesCurrentDirectory")]
public class when_adding_cratis_arc_with_identity_provider_override_winning_over_configuration : Specification
{
    Type _resolvedProviderType;

    void Because()
    {
        if (!Directory.Exists(Environment.CurrentDirectory))
        {
            Environment.CurrentDirectory = AppContext.BaseDirectory;
        }

        var configurationPath = ConfigurationPath.Combine(HostBuilderExtensions.DefaultSectionPaths);
        var configurationValues = new Dictionary<string, string?>
        {
            [$"{configurationPath}:IdentityDetailsProvider"] = typeof(ConfiguredIdentityDetailsProvider).AssemblyQualifiedName
        };

        var builder = new ArcApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(configurationValues);
        builder.AddCratisArc(options => options.IdentityDetailsProvider = typeof(OverrideIdentityDetailsProvider));

        using var app = builder.Build();
        var provider = app.Services.GetRequiredService<IProvideIdentityDetails>();
        _resolvedProviderType = provider.GetType();
    }

    [Fact] void should_use_provider_from_code_override() => _resolvedProviderType.ShouldEqual(typeof(OverrideIdentityDetailsProvider));

    class ConfiguredIdentityDetailsProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "Configured" }));
    }

    class OverrideIdentityDetailsProvider : IProvideIdentityDetails
    {
        public Task<IdentityDetails> Provide(IdentityProviderContext context) => Task.FromResult(new IdentityDetails(true, new { Name = "Override" }));
    }
}
