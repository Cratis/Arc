// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_arc_has_custom_serializer_options : an_mcp_host
{
    JsonElement _schema;
    McpResponse _response;

    protected override void ConfigureArcOptions(ArcOptions options)
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.Converters.Insert(0, new NotePriorityConverter());
    }

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.Expose<WriteFormattedNote>("write_formatted_note", "Writes a formatted note.");

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        var listing = await ListTools();
        _schema = listing.Message.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(_ => _.GetProperty("name").GetString() == "write_formatted_note").GetProperty("inputSchema");
        _response = await CallTool("write_formatted_note", """{"note_text":"hello","priority":"urgent"}""");
    }

    [Fact] void should_advertise_the_configured_property_name() => _schema.GetProperty("properties").TryGetProperty("note_text", out _).ShouldBeTrue();
    [Fact] void should_not_advertise_the_default_property_name() => _schema.GetProperty("properties").TryGetProperty("noteText", out _).ShouldBeFalse();
    [Fact] void should_not_override_the_custom_converter_schema_with_default_enum_names() => _schema.GetProperty("properties").GetProperty("priority").ValueKind.ShouldEqual(JsonValueKind.True);
    [Fact] void should_succeed() => _response.IsToolError.ShouldBeFalse();
    [Fact] void should_dispatch_once() => _probe.Count.ShouldEqual(1);
    [Fact] void should_bind_the_advertised_arguments() => _probe.Dispatched.Single().Text.ShouldEqual("hello:VeryHigh");
}
