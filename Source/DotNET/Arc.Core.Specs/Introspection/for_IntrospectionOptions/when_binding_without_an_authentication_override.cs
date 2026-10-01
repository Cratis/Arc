// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Introspection.for_IntrospectionOptions;

public class when_binding_without_an_authentication_override : Specification
{
    IntrospectionOptions _options = new();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Enabled"] = "true" })
        .Build().Bind(_options);

    [Fact] void should_keep_the_environment_default_outside_development() => _options.RequiresAuthentication(isDevelopment: false).ShouldBeTrue();
    [Fact] void should_keep_the_environment_default_in_development() => _options.RequiresAuthentication(isDevelopment: true).ShouldBeFalse();
    [Fact] void should_not_treat_binding_as_an_explicit_override() => _options.AuthenticationExplicitlyDisabled.ShouldBeFalse();
}
