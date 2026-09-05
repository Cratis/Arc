// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class and_fixed_tenancy_is_configured : Specification
{
    const string TenantId = "acme";

    ITenantIdResolver _resolver;

    void Because()
    {
        using var serviceProvider = new ServiceCollection()
            .AddCratisArcCore()
            .Configure<ArcOptions>(options => options.UseFixedTenancy(TenantId))
            .BuildServiceProvider();

        _resolver = serviceProvider.GetRequiredService<ITenantIdResolver>();
    }

    [Fact] void should_resolve_the_fixed_tenant_id_resolver() => _resolver.ShouldBeOfExactType<FixedTenantIdResolver>();
    [Fact] void should_resolve_the_configured_tenant_id() => _resolver.Resolve().ShouldEqual(TenantId);
}
