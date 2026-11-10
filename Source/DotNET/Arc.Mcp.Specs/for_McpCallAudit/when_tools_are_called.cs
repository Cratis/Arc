// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Cratis.Arc.Mcp.given;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp.for_McpCallAudit;

public class when_tools_are_called : an_mcp_host
{
    const string SecretArgument = "dpat_spec_private_argument";
    readonly LogRecorder<McpCallAudit> _logs = new();
    readonly ToolGate _gate = new();
    object?[] _withoutPerson;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ILogger<McpCallAudit>>(_logs);
        services.AddSingleton(_gate);
    }

    protected override void ConfigureMcp(McpBuilder mcp) => mcp
        .ExposeQuery<NoteReadModel>(nameof(NoteReadModel.All), "list_notes", "Lists notes.")
        .ExposeQuery<NoteReadModel>(nameof(NoteReadModel.Stream), "stream_notes", "Streams notes.")
        .AddToolSource<RichTool>()
        .AddToolSource<ThrowingTool>()
        .AddToolAuthorizer<DenyingAuthorizer>();

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope, WriteScope);
        await ListTools();
        await CallTool("write_note", "{\"text\":\"" + SecretArgument + "\"}");
        await CallTool("list_notes", "{}");
        await CallTool("who_am_i", "{}");
        await CallTool("stream_notes", "{}");
        await CallTool("write_note", "{\"text\":{}}");
        await CallTool("throwing_tool", "{}");
        await CallTool("missing_structure", "{}");
        _gate.Denied.Add("write_note");
        await CallTool("write_note", "{\"text\":\"" + SecretArgument + "\"}");
        _gate.Denied.Clear();
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        await CallTool("write_note", "{\"text\":\"" + SecretArgument + "\"}");
        RecordWithoutPerson();
    }

    [Fact] void should_record_each_call_once_and_not_the_listing() => _logs.Entries.Count.ShouldEqual(9);
    [Fact] void should_use_information_level() => _logs.Entries.Select(_ => _.Level).Distinct().ShouldContainOnly(LogLevel.Information);
    [Fact] void should_record_outcomes_in_call_order() => _logs.Entries.Select(_ => _.Values[6]).ShouldEqual(["succeeded", "succeeded", "succeeded", "failed", "invalid_arguments", "error", "malformed", "not_allowed", "insufficient_scope"]);
    [Fact] void should_record_the_command_kind() => _logs.Entries[0].Values[1].ShouldEqual(McpToolKind.Command);
    [Fact] void should_record_the_query_kind() => _logs.Entries[1].Values[1].ShouldEqual(McpToolKind.Query);
    [Fact] void should_record_the_custom_kind() => _logs.Entries[2].Values[1].ShouldEqual(McpToolKind.Custom);
    [Fact] void should_record_the_tool() => _logs.Entries[0].Values[0].ShouldEqual("write_note");
    [Fact] void should_record_the_person() => _logs.Entries[0].Values[2].ShouldEqual("person");
    [Fact] void should_record_the_tenant() => _logs.Entries[0].Values[3].ShouldEqual("tenant-a");
    [Fact] void should_record_the_non_secret_credential_id() => _logs.Entries[0].Values[4].ShouldEqual("credential");
    [Fact] void should_record_the_credential_kind() => _logs.Entries[0].Values[5].ShouldEqual(CallerCredentialKind.PersonalAccessToken);
    [Fact] void should_record_a_correlation_id() => _logs.Entries.TrueForAll(_ => !string.IsNullOrWhiteSpace(_.Values[7]?.ToString())).ShouldBeTrue();
    [Fact] void should_not_log_argument_values() => LoggedValues().Any(_ => _.Contains(SecretArgument, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_log_a_bearer_token() => LoggedValues().Any(_ => _.Contains("Bearer ", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_allow_a_caller_without_a_person() => _withoutPerson[2].ShouldEqual(string.Empty);

    IEnumerable<string> LoggedValues() => _logs.Entries.SelectMany(_ => _.Values.Select(value => value?.ToString() ?? string.Empty).Append(_.Message));

    void RecordWithoutPerson()
    {
        var logger = new LogRecorder<McpCallAudit>();
        var accessor = Substitute.For<ICorrelationIdAccessor>();
        accessor.Current.Returns(CorrelationId.New());
        var caller = VerifiedCallerFor(CallerCredentialKind.PersonalAccessToken, "tenant-a", ReadScope) with { Person = null };
        new McpCallAudit(logger, accessor).Record(McpToolDescriptor.Custom("service_tool", "Service.", [ReadScope], McpToolAccess.Read), new(null!, caller), "succeeded");
        _withoutPerson = logger.Entries.Single().Values;
    }
}
