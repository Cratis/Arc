// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public class a_missing_metadata_command_host : a_protected_command_host
{
    protected override Type DecisionCommand => typeof(UnregisteredStockReservation);

    protected override void ConfigureMcp(McpBuilder mcp) =>
        mcp.Expose<UnregisteredStockReservation>("reserve_stock", "Reserves stock without generated command key metadata.");
}
