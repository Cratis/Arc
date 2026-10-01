// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionOptions;

public class when_reloading_an_authentication_override : Specification
{
    bool _initialRequiresAuthentication;
    bool _overriddenRequiresAuthentication;
    bool _restoredRequiresAuthentication;

    void Because()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Introspection:Enabled"] = "true" })
            .Build();
        var services = new ServiceCollection();
        ArcOptionsConfiguration.Configure(services, configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<ArcOptions>>();
        _initialRequiresAuthentication = options.CurrentValue.Introspection.RequiresAuthentication(isDevelopment: false);
        configuration["Introspection:RequireAuthentication"] = "false";
        configuration.Reload();
        _overriddenRequiresAuthentication = options.CurrentValue.Introspection.RequiresAuthentication(isDevelopment: false);
        configuration["Introspection:RequireAuthentication"] = null;
        configuration.Reload();
        _restoredRequiresAuthentication = options.CurrentValue.Introspection.RequiresAuthentication(isDevelopment: false);
    }

    [Fact] void should_start_with_the_secure_environment_default() => _initialRequiresAuthentication.ShouldBeTrue();
    [Fact] void should_apply_the_explicit_configuration_override() => _overriddenRequiresAuthentication.ShouldBeFalse();
    [Fact] void should_restore_the_environment_default_when_the_key_is_removed() => _restoredRequiresAuthentication.ShouldBeTrue();
}
