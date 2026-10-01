// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Introspection.for_IntrospectionOptions;

public class when_binding_an_explicit_anonymous_override : Specification
{
    IntrospectionOptions _options = new();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["RequireAuthentication"] = "false" })
        .Build().Bind(_options);

    [Fact] void should_preserve_the_boolean_consumer_api() => _options.RequireAuthentication.ShouldBeFalse();
    [Fact] void should_disable_authentication_outside_development() => _options.RequiresAuthentication(isDevelopment: false).ShouldBeFalse();
}
