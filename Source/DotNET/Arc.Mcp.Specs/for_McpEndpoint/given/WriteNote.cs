// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// An exposed command.
/// </summary>
/// <param name="Text">The note.</param>
public record WriteNote(string Text)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="probe">The probe.</param>
    /// <param name="callers">The capability callers.</param>
    public void Handle(McpProbe probe, ICapabilityCallers callers) => probe.Record(nameof(WriteNote), Text, callers.Current);
}
