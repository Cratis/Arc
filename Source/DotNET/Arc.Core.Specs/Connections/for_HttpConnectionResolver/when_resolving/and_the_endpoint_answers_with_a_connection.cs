// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Arc.Connections.for_HttpConnectionResolver.when_resolving;

public class and_the_endpoint_answers_with_a_connection : given.an_http_connection_resolver
{
    ResolvedConnection _result;

    void Establish() => _response = new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            """{ "database": "tenant-acme", "identity": { "accessToken": "the-token" }, "expiresAt": "2026-10-15T12:00:00+00:00" }""",
            Encoding.UTF8,
            "application/json")
    };

    async Task Because() => _result = await _resolver.Resolve(_request);

    [Fact] void should_post_to_the_endpoint() => _sentRequest!.RequestUri.ShouldEqual(_endpoint);
    [Fact] void should_post() => _sentRequest!.Method.ShouldEqual(HttpMethod.Post);
    [Fact] void should_send_the_configured_headers() => _sentRequest!.Headers.GetValues("X-Api-Key").ShouldContainOnly("the-key");
    [Fact] void should_send_the_storage() => _sentBody.ShouldContain("\"storage\":\"SqlServer\"");
    [Fact] void should_send_the_purpose_by_name() => _sentBody.ShouldContain("\"purpose\":\"Runtime\"");
    [Fact] void should_send_the_scope() => _sentBody.ShouldContain("\"scope\":{\"tenant\":\"acme\"}");
    [Fact] void should_return_the_database() => _result.Database.ShouldEqual("tenant-acme");
    [Fact] void should_return_the_access_token() => _result.Identity!.AccessToken.ShouldEqual("the-token");
    [Fact] void should_not_set_a_connection_string() => _result.ConnectionString.ShouldBeNull();
    [Fact] void should_return_the_expiry() => _result.ExpiresAt.ShouldEqual(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero));
}
