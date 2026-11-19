// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.when_resolving_tenant_header;

public class with_identical_joined_values : given.a_header_tenant_resolver
{
    string _result;

    void Establish() => _httpContext.Request.Headers[Constants.DefaultTenantIdHeader] = "acme,acme";
    void Because() => _result = _resolver.Resolve();

    [Fact] void should_resolve_the_single_tenant() => _result.ShouldEqual("acme");
}
