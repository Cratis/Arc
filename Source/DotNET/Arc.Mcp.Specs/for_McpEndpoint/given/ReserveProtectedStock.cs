// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Makes a protected decision through the real MCP command pipeline.
/// </summary>
/// <param name="Id">The stock to reserve.</param>
[Command, ReadProtection(ReadProtectionMode.On)]
public record ReserveProtectedStock(EventSourceId Id)
{
    /// <summary>
    /// Returns the event after the protected read, permitting a deterministic competing append.
    /// </summary>
    /// <param name="stock">The protected stock model.</param>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The reservation event.</returns>
    public async Task<StockReserved> Handle(ProtectedStock stock, ProtectedCommandProbe probe)
    {
        probe.Handles++;
        probe.ReadProtection = CommandDecisionPolicy.ReadProtection;
        await probe.BeforeReturn();
        return new(stock.Available);
    }
}
