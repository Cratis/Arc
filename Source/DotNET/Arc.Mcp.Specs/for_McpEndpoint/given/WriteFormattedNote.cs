// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public record WriteFormattedNote(string NoteText, NotePriority Priority)
{
    public void Handle(McpProbe probe, ICapabilityCallers callers) => probe.Record(nameof(WriteFormattedNote), $"{NoteText}:{Priority}", callers.Current);
}
