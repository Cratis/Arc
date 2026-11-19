// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_protected_command_keys_are_invalid_or_missing : a_protected_command_host
{
    static readonly string[] _keyValues = ["synthetic-invalid-stock", "synthetic-missing-stock", Stock.Value];
    McpResponse[] _responses;
    JsonElement[] _structured;

    async Task Because()
    {
        await SeedStock();
        _responses =
        [
            await CallTool("reserve_stock", "{\"id\":\"synthetic-invalid-stock#leaf\"}"),
            await CallTool("reserve_stock", "{}"),
            await CallTool("reserve_stock", "{\"id\":\"synthetic-missing-stock\"}")
        ];
        _structured = _responses.Select(response => response.Message.GetProperty("result"))
            .Where(result => result.TryGetProperty("structuredContent", out _))
            .Select(result => result.GetProperty("structuredContent")).ToArray();
    }

    [Fact] void should_refuse_all_three_calls() => _responses.All(response => response.IsToolError).ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => _decision.Handles.ShouldEqual(0);
    [Fact] void should_not_commit_owner_events() => _appended.OfType<StockReserved>().ShouldBeEmpty();
    [Fact] void should_not_echo_key_values_anywhere_in_the_full_response() =>
        _responses.Where(response => !HasNoKeyValues(response.Body)).Select(response => response.Body).ShouldBeEmpty();
    [Fact] void should_not_echo_key_values_in_the_decoded_payload() =>
        _responses.Where(response => !HasNoKeyValues(response.ToolText)).Select(response => response.ToolText).ShouldBeEmpty();
    [Fact] void should_capture_structured_arc_outcomes() => _structured.ShouldNotBeEmpty();
    [Fact] void should_not_echo_key_values_in_the_structured_payload() =>
        _structured.Where(payload => !HasNoKeyValues(payload.GetRawText())).ShouldBeEmpty();
    [Fact] void should_capture_the_real_mcp_audit_logs() => _logs.Entries.Any(entry => entry.Category == typeof(McpCallAudit).FullName).ShouldBeTrue();
    [Fact] void should_not_log_key_values_in_messages_exceptions_or_structured_state() =>
        _logs.Entries.Where(entry => !HasNoKeyValues(entry.Content)).ShouldBeEmpty();

    static bool HasNoKeyValues(string text) => _keyValues.All(key => !text.Contains(key, StringComparison.Ordinal));
}
