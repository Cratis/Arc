// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_resolving_tenant_header;

public class with_identical_joined_values : given.a_header_tenant_resolver
{
    string _result;

    Task Establish() => SendTenantHeaders("acme,acme");
    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_single_tenant() => _result.ShouldEqual("acme");
}
