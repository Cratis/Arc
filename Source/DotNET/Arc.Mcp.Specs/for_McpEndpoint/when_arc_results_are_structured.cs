// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_arc_results_are_structured : an_mcp_host
{
    McpResponse _command;
    McpResponse _failedCommand;
    McpResponse _query;
    McpResponse _failedQuery;
    McpResponse _listed;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp
        .Expose<ReplyToNote>("reply_note", "Replies.")
        .ExposeQuery($"{typeof(StructuredReadModel).FullName}.{nameof(StructuredReadModel.All)}", "structured_notes", "Lists notes.");

    protected override void ConfigureServices(IServiceCollection services)
    {
        var handlers = new[] { typeof(WriteNote), typeof(ReplyToNote) }
            .Select(type => (ICommandHandler)new ModelBoundCommandHandler(type, type.GetMethod("Handle")!)).ToArray();
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns(handlers);
        commands.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var found = handlers.SingleOrDefault(handler => handler.CommandType == call[0]!.GetType());
            call[1] = found;
            return found is not null;
        });
        services.AddSingleton(commands);
    }

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        _listed = await ListTools();
        _command = await CallTool("reply_note", "{\"text\":\"hello\"}");
        _failedCommand = await CallTool("reply_note", "{\"text\":\"fail\"}");
        _query = await CallTool("structured_notes", "{\"page\":1,\"page_size\":2}");
        _failedQuery = await CallTool("structured_notes", "{\"fail\":true}");
    }

    [Fact] void should_keep_command_text_and_structured_content_equal() => JsonElement.DeepEquals(JsonDocument.Parse(_command.ToolText).RootElement, Structured(_command)).ShouldBeTrue();
    [Fact] void should_keep_query_text_and_structured_content_equal() => JsonElement.DeepEquals(JsonDocument.Parse(_query.ToolText).RootElement, Structured(_query)).ShouldBeTrue();
    [Fact] void should_return_the_command_response() => Structured(_command).GetProperty("response").GetString().ShouldEqual("reply:hello");
    [Fact] void should_return_paging() => Structured(_query).GetProperty("paging").GetProperty("size").GetInt32().ShouldEqual(2);
    [Fact] void should_return_query_data() => Structured(_query).GetProperty("data").GetArrayLength().ShouldEqual(1);
    [Fact] void should_mark_a_command_pipeline_failure() => _failedCommand.IsToolError.ShouldBeTrue();
    [Fact] void should_mark_a_query_pipeline_failure() => _failedQuery.IsToolError.ShouldBeTrue();
    [Fact] void should_suppress_a_failed_command_response() => Structured(_failedCommand).GetProperty("response").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_suppress_failed_query_data() => Structured(_failedQuery).GetProperty("data").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_keep_failure_text_and_structure_equal() => JsonElement.DeepEquals(JsonDocument.Parse(_failedQuery.ToolText).RootElement, Structured(_failedQuery)).ShouldBeTrue();
    [Fact] void should_describe_the_command_output() => OutputSchema("reply_note").GetProperty("properties").GetProperty("response").ValueKind.ShouldEqual(JsonValueKind.Object);
    [Fact] void should_describe_the_query_output() => OutputSchema("structured_notes").GetProperty("properties").GetProperty("data").ValueKind.ShouldEqual(JsonValueKind.Object);
    [Fact] void should_describe_outcome_as_an_object() => OutputSchema("reply_note").GetProperty("properties").GetProperty("outcome").GetProperty("type").GetString().ShouldEqual("object");

    static JsonElement Structured(McpResponse response) => response.Json!.Value.GetProperty("result").GetProperty("structuredContent");

    JsonElement OutputSchema(string tool) => _listed.Json!.Value.GetProperty("result").GetProperty("tools").EnumerateArray()
        .Single(_ => _.GetProperty("name").GetString() == tool).GetProperty("outputSchema");
}
