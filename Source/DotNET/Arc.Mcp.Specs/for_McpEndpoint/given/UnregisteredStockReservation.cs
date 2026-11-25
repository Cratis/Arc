// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// An explicitly provided handler without a Command attribute, so no command key metadata is generated.
/// </summary>
/// <param name="Id">The stock identity.</param>
[ReadProtection(ReadProtectionMode.On)]
public record UnregisteredStockReservation(EventSourceId Id)
{
    /// <summary>Makes the same protected decision using reflection compatibility when explicitly enabled.</summary>
    /// <param name="stock">The protected stock model.</param>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The reservation event.</returns>
    public StockReserved Handle(ProtectedStock stock, ProtectedCommandProbe probe)
    {
        probe.Handles++;
        probe.ReadProtection = CommandDecisionPolicy.ReadProtection;
        return new(stock.Available);
    }
}
