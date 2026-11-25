// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_generated_command_key_metadata_is_missing : a_missing_metadata_command_host
{
    Exception _refusal;

    protected override async Task StartHost()
    {
        try
        {
            await base.StartHost();
        }
        catch (Exception exception)
        {
            _refusal = exception;
        }
    }

    [Fact] void should_fail_closed_at_startup() => _refusal.ShouldBeOfExactType<CommandReadModelKeysMissing>();
    [Fact] void should_not_start_the_mcp_host() => _app.Lifetime.ApplicationStarted.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_not_invoke_the_handler() => _decision.Handles.ShouldEqual(0);
    [Fact] void should_not_append_events() => _appended.ShouldBeEmpty();
    [Fact] void should_not_put_key_values_in_the_refusal() => _refusal.Message.ShouldNotContain(Stock.Value);
    [Fact] void should_not_put_key_values_in_logs() => _logs.Entries.ShouldEachConformTo(entry => !entry.Content.Contains(Stock.Value, StringComparison.Ordinal));
}
