// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_users_request;

public class and_no_users_are_provided : given.an_identity_schema_endpoint_handler
{
    void Establish() => MapIdentityProviderEndpoint();

    async Task Because() => await _mappedHandlers["/.cratis/users"](_httpRequestContext);

    [Fact] void should_set_no_store_cache_control() => _httpRequestContext.Received(1).SetResponseHeader("Cache-Control", "no-store, private");
    [Fact] void should_vary_on_cookie() => _httpRequestContext.Received(1).SetResponseHeader("Vary", "Cookie");

    [Fact]
    void should_write_empty_users_collection() => _httpRequestContext.Received(1).WriteResponseAsJson(
        Arg.Is<object>(value => value != null && !((IEnumerable<User>)value).Any()),
        typeof(IEnumerable<User>),
        CancellationToken.None);
}
