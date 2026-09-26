// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Introspection.for_IntrospectionOptions;

public class when_binding_from_configuration : Specification
{
    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:Introspection:Enabled"] = "false",
            ["Cratis:Arc:Introspection:RequireAuthentication"] = "true",
            ["Cratis:Arc:Introspection:Roles"] = "Administrator,Operator"
        })
        .Build().GetSection("Cratis:Arc").Bind(_options);

    [Fact] void should_disable_endpoints() => _options.Introspection.Enabled.ShouldBeFalse();
    [Fact] void should_require_authentication() => _options.Introspection.RequireAuthentication.ShouldBeTrue();
    [Fact] void should_bind_roles() => _options.Introspection.Roles.ShouldEqual("Administrator,Operator");
}
