// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Chronicle.Tenancy.for_TenantNamespaceResolver.when_resolving;

public class and_tenant_id_contains_a_comma : Specification
{
    TenantNamespaceResolver _resolver;
    Exception _result;

    void Establish()
    {
        var tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        tenantIdAccessor.Current.Returns(new TenantId("acme,acme"));
        _resolver = new TenantNamespaceResolver(tenantIdAccessor);
    }

    void Because() => _result = Catch.Exception(() => _resolver.Resolve());

    [Fact] void should_refuse_it_as_a_namespace() => _result.ShouldBeOfExactType<TenantIdIsNotAValidNamespace>();
}
