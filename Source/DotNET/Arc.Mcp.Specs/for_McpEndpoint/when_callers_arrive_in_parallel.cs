// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_callers_arrive_in_parallel : an_mcp_host
{
    const int Rounds = 12;

    List<McpResponse> _readers;
    List<McpResponse> _writers;

    async Task Because()
    {
        var requests = _app.Services.GetRequiredService<IHttpRequestContextAccessor>();
        _verified.GetCurrent().Returns(_ =>
        {
            var writer = requests.Current?.Headers.TryGetValue("x-spec-caller", out var who) == true && who == "writer";
            var scopes = writer ? new[] { ReadScope, WriteScope } : [ReadScope];
            return new CallerResolution(VerifiedCallerFor(CallerCredentialKind.PersonalAccessToken, writer ? "tenant-w" : "tenant-r", scopes), null);
        });

        var readers = Enumerable.Range(0, Rounds).Select(_ => ListTools(request => request.Headers.TryAddWithoutValidation("x-spec-caller", "reader")));
        var writers = Enumerable.Range(0, Rounds).Select(_ => ListTools(request => request.Headers.TryAddWithoutValidation("x-spec-caller", "writer")));
        var all = await Task.WhenAll(readers.Concat(writers).ToArray());
        _readers = [.. all.Take(Rounds)];
        _writers = [.. all.Skip(Rounds)];
    }

    [Fact] void should_list_only_the_read_tools_for_every_reader() => _readers.TrueForAll(_ => _.ToolNames.Count() == 1 && _.ToolNames.Single() == "who_am_i").ShouldBeTrue();
    [Fact] void should_list_every_tool_for_every_writer() => _writers.TrueForAll(_ => _.ToolNames.Count() == 2).ShouldBeTrue();
}
