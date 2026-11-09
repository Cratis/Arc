// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpOptIn.given;

namespace Cratis.Arc.Mcp.for_McpOptIn;

public class when_mcp_is_enabled_without_capabilities : an_opt_in_host
{
    Exception? _error;

    void Establish() => Build(true);

    async Task Because()
    {
        try
        {
            await _app.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    [Fact] void should_fail_closed_at_startup() => _error.ShouldBeOfExactType<McpRequiresCapabilities>();
    [Fact] void should_explain_the_missing_hosting_requirement() => _error!.Message.ShouldContain("WithCapabilities");
}
