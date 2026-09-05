// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_schema_request;

public class and_provider_is_not_registered : given.an_identity_schema_endpoint_handler
{
    void Establish() => MapIdentityProviderEndpoint();

    async Task Because() => await _mappedHandlers["/.cratis/identity-details/schema"](_httpRequestContext);

    [Fact]
    void should_write_empty_schema() => _httpRequestContext.Received(1).WriteResponseAsJson(
        Arg.Is<object>(value => value != null && ((JsonObject)value).Count == 0),
        typeof(JsonObject),
        CancellationToken.None);
}
