// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection.for_DiscoveryExposure;

[Collection("UsesCurrentDirectory")]
public class when_only_catalogs_are_enabled_without_authentication : given.a_listener_host
{
    string _warning;

    async Task Because()
    {
        await RequestDiscovery(["--environment", "Production"], options => options.Introspection.IdentityDiscovery = false);
        _warning = _warnings.Single(message => message.Contains("are not mapped", StringComparison.Ordinal));
    }

    [Fact] void should_name_commands() => _warning.Contains("/.cratis/commands", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_queries() => _warning.Contains("/.cratis/queries", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_name_users() => _warning.Contains("/.cratis/users", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_name_tenants() => _warning.Contains("/.cratis/tenants", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_name_the_identity_schema() => _warning.Contains("/.cratis/identity-details/schema", StringComparison.Ordinal).ShouldBeFalse();
}
