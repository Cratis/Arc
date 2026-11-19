// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Exercises the filter's raw-read boundary only for the protected MCP command.
/// </summary>
public class ProtectedRawReadFilter : ICommandFilter
{
    /// <inheritdoc/>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        if (context.Command is ReserveProtectedStock)
        {
            var probe = context.ServiceProvider!.GetRequiredService<ProtectedCommandProbe>();
            probe.Filters++;
            if (probe.ReadInFilter)
            {
                await ReadRaw(context, probe);
            }
        }

        return CommandResult.Success(context.CorrelationId);
    }

    static async Task ReadRaw(CommandContext context, ProtectedCommandProbe probe)
    {
        try
        {
            await context.ServiceProvider!.GetRequiredService<IEventStore>().EventLog.GetTailSequenceNumber();
        }
        catch (ReadRefusedWhileReadsAreProtected)
        {
            probe.RawReadRefused = true;
            throw;
        }
    }
}
