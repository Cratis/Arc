// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_the_origin_is_not_allowed : an_mcp_host
{
    McpResponse _withoutOrigin;
    McpResponse _allowed;
    McpResponse _allowedWithOtherCasing;
    McpResponse _foreign;
    McpResponse _opaque;
    McpResponse _twice;
    McpResponse _withPath;
    int _dispatchedWhenRefused;

    protected override void ConfigureOptions(McpOptions options) => options.AllowedOrigins.Add("https://app.example.test");

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _withoutOrigin = await Write(null);
        _allowed = await Write("https://app.example.test");
        _allowedWithOtherCasing = await Write("HTTPS://APP.example.test");
        var allowedDispatches = _probe.Count;
        _foreign = await Write("https://evil.example.test");
        _opaque = await Write("null");
        _twice = await Post("""{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}""", customize: request => request.Headers.TryAddWithoutValidation("Origin", ["https://app.example.test", "https://evil.example.test"]));
        _withPath = await Write("https://app.example.test/path");
        _dispatchedWhenRefused = _probe.Count - allowedDispatches;
    }

    [Fact] void should_allow_a_request_without_an_origin() => _withoutOrigin.IsToolError.ShouldBeFalse();
    [Fact] void should_allow_a_configured_origin() => _allowed.IsToolError.ShouldBeFalse();
    [Fact] void should_compare_origins_case_insensitively() => _allowedWithOtherCasing.IsToolError.ShouldBeFalse();
    [Fact] void should_refuse_another_origin_as_forbidden() => _foreign.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_name_the_refusal() => _foreign.RefusalError.ShouldEqual("origin_not_allowed");
    [Fact] void should_refuse_an_opaque_origin() => _opaque.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_refuse_more_than_one_origin() => _twice.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_refuse_an_origin_that_is_not_an_origin() => _withPath.Status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_dispatch_a_refused_call() => _dispatchedWhenRefused.ShouldEqual(0);

    Task<McpResponse> Write(string? origin) => Post(
        """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"write_note","arguments":{"text":"x"}}}""",
        customize: request =>
        {
            if (origin is not null)
            {
                request.Headers.TryAddWithoutValidation("Origin", origin);
            }
        });
}
