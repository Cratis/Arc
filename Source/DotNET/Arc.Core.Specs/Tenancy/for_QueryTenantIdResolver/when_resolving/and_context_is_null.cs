// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_QueryTenantIdResolver.when_resolving;

public class and_context_is_null : given.a_query_tenant_id_resolver
{
    string _result;

    void Establish()
    {
        _httpRequestContextAccessor.Current.Returns(_ => null);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_empty_string() => _result.ShouldEqual(string.Empty);
}
