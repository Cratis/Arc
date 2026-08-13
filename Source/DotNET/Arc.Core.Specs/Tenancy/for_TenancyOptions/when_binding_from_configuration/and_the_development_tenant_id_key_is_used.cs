// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Tenancy.for_TenancyOptions.when_binding_from_configuration;

public class and_the_development_tenant_id_key_is_used : Specification
{
    const string TenantId = "local-tenant";

    ArcOptions _options;

    void Establish() => _options = new ArcOptions();

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tenancy:ResolverType"] = "Development",
            ["Tenancy:DevelopmentTenantId"] = TenantId
        })
        .Build()
        .Bind(_options);

    [Fact] void should_bind_the_resolver_type() => _options.Tenancy.ResolverType.ShouldEqual(TenantResolverType.Development);
    [Fact] void should_bind_the_development_tenant_id() => _options.Tenancy.DevelopmentTenantId.ShouldEqual(TenantId);
    [Fact] void should_expose_the_same_value_under_the_fixed_name() => _options.Tenancy.FixedTenantId.ShouldEqual(TenantId);
}
