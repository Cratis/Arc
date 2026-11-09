// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_a_query_has_a_custom_argument_converter : an_mcp_host
{
    JsonElement _schema;
    McpResponse _urgent;
    McpResponse _deferred;
    McpResponse _invalid;

    protected override void ConfigureArcOptions(ArcOptions options)
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Insert(0, new NotePriorityConverter());
    }

    protected override void ConfigureMcp(McpBuilder mcp) =>
        mcp.ExposeQuery($"{typeof(NoteReadModel).FullName}.{nameof(NoteReadModel.ByPriority)}", "priority_note", "Gets a note by priority.");

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        var listing = await ListTools();
        _schema = listing.Message.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(_ => _.GetProperty("name").GetString() == "priority_note").GetProperty("inputSchema");
        _urgent = await CallTool("priority_note", """{"note_priority":"urgent"}""");
        _deferred = await CallTool("priority_note", """{"note_priority":"deferred"}""");
        _invalid = await CallTool("priority_note", """{"note_priority":"VeryHigh"}""");
    }

    [Fact] void should_advertise_the_converter_input() => _schema.GetProperty("properties").GetProperty("note_priority").ValueKind.ShouldEqual(JsonValueKind.True);
    [Fact] void should_execute_with_the_custom_urgent_value() => (_urgent.IsToolError, _urgent.ToolText.Contains("VeryHigh")).ShouldEqual((false, true));
    [Fact] void should_execute_with_the_custom_deferred_value() => (_deferred.IsToolError, _deferred.ToolText.Contains("VeryLow")).ShouldEqual((false, true));
    [Fact] void should_reject_values_the_converter_does_not_accept() => _invalid.IsToolError.ShouldBeTrue();
    [Fact] void should_dispatch_only_the_valid_calls() => _probe.Count.ShouldEqual(2);
    [Fact] void should_pass_the_deserialized_priority_to_the_query() => _probe.Dispatched.Select(_ => _.Text).ShouldContainOnly("VeryHigh", "VeryLow");
}
