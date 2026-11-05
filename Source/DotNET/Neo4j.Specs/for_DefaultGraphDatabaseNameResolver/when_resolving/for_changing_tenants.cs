// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Neo4j.for_DefaultGraphDatabaseNameResolver.when_resolving;

public class for_changing_tenants : Specification
{
    IOptionsMonitor<Neo4jOptions> _options;
    ITenantIdAccessor _accessor;
    DefaultGraphDatabaseNameResolver _resolver;
    string _notSet;
    string _default;
    string _acme;
    string _globex;

    void Establish()
    {
        _options = Substitute.For<IOptionsMonitor<Neo4jOptions>>();
        _options.CurrentValue.Returns(new Neo4jOptions { EventStore = "Orders" });
        _accessor = Substitute.For<ITenantIdAccessor>();
        _resolver = new(_options, new TenantGraphNamespaceResolver(_accessor));
    }

    void Because()
    {
        _accessor.Current.Returns(TenantId.NotSet);
        _notSet = _resolver.Resolve();
        _accessor.Current.Returns(TenantId.Default);
        _default = _resolver.Resolve();
        _accessor.Current.Returns(new TenantId("acme"));
        _acme = _resolver.Resolve();
        _accessor.Current.Returns(new TenantId("globex"));
        _globex = _resolver.Resolve();
    }

    [Fact] void should_use_the_bare_event_store_for_an_unset_tenant() => _notSet.ShouldEqual("orders");
    [Fact] void should_use_the_bare_event_store_for_the_default_tenant() => _default.ShouldEqual("orders");
    [Fact] void should_use_the_database_of_the_first_tenant() => _acme.ShouldEqual("orders-acme");
    [Fact] void should_follow_the_tenant_to_another_database() => _globex.ShouldEqual("orders-globex");
}
