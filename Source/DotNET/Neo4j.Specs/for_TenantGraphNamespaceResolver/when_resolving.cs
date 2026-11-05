// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Neo4j.for_TenantGraphNamespaceResolver;

public class when_resolving : Specification
{
    ITenantIdAccessor _accessor;
    TenantGraphNamespaceResolver _resolver;
    string _notSet;
    string _default;
    string _named;

    void Establish()
    {
        _accessor = Substitute.For<ITenantIdAccessor>();
        _resolver = new(_accessor);
    }

    void Because()
    {
        _accessor.Current.Returns(TenantId.NotSet);
        _notSet = _resolver.Resolve();
        _accessor.Current.Returns(TenantId.Default);
        _default = _resolver.Resolve();
        _accessor.Current.Returns(new TenantId("acme"));
        _named = _resolver.Resolve();
    }

    [Fact] void should_use_the_default_namespace_for_an_unset_tenant() => _notSet.ShouldEqual(GraphDatabaseName.DefaultNamespace);
    [Fact] void should_use_the_default_namespace_for_the_default_tenant() => _default.ShouldEqual(GraphDatabaseName.DefaultNamespace);
    [Fact] void should_use_the_tenant_as_the_namespace() => _named.ShouldEqual("acme");
}
