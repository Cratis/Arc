// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_serving_protected_resource_metadata : an_mcp_host
{
    McpResponse _metadata;
    McpResponse _metadataWithForgedHost;

    protected override McpProfile Profile => McpProfile.OAuthOnly;

    async Task Because()
    {
        Unauthenticate();
        _metadata = await Get("/.well-known/oauth-protected-resource/mcp");
        _metadataWithForgedHost = await Get("/.well-known/oauth-protected-resource/mcp", "attacker.example.test");
    }

    [Fact] void should_serve_without_a_caller() => _metadata.Status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_name_the_configured_resource() => _metadata.Message.GetProperty("resource").GetString().ShouldEqual($"{PublicOrigin}/mcp");
    [Fact] void should_name_the_authorization_servers() => _metadata.Message.GetProperty("authorization_servers").EnumerateArray().Select(_ => _.GetString()).ShouldContainOnly("https://login.example.test");
    [Fact] void should_name_the_scopes() => _metadata.Message.GetProperty("scopes_supported").EnumerateArray().Select(_ => _.GetString()).ShouldContainOnly(ReadScope, WriteScope);
    [Fact] void should_ignore_a_forged_host() => _metadataWithForgedHost.Body.ShouldEqual(_metadata.Body);
    [Fact] void should_not_echo_the_forged_host() => _metadataWithForgedHost.Body.Contains("attacker", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_answer_with_a_json_object() => _metadata.Message.ValueKind.ShouldEqual(JsonValueKind.Object);
}
