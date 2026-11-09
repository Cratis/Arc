// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A read model with a query that the specs that need it expose over MCP explicitly.
/// </summary>
/// <param name="Text">The note.</param>
[ReadModel]
public record NoteReadModel(string Text)
{
    /// <summary>
    /// Gets the notes, optionally those starting with a prefix.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="probe">The probe.</param>
    /// <param name="callers">The capability callers.</param>
    /// <returns>The notes.</returns>
    public static IQueryable<NoteReadModel> All(string? prefix, McpProbe probe, ICapabilityCallers callers)
    {
        probe.Record("list_notes", prefix, callers.Current);
        return new[] { new NoteReadModel("alpha"), new NoteReadModel("beta"), new NoteReadModel("gamma") }
            .Where(_ => prefix is null || _.Text.StartsWith(prefix, StringComparison.Ordinal))
            .AsQueryable();
    }

    /// <summary>
    /// Gets a note selected by priority.
    /// </summary>
    /// <param name="notePriority">The priority.</param>
    /// <param name="probe">The probe.</param>
    /// <param name="callers">The capability callers.</param>
    /// <returns>The matching note.</returns>
    public static NoteReadModel ByPriority(NotePriority notePriority, McpProbe probe, ICapabilityCallers callers)
    {
        probe.Record("priority_note", notePriority.ToString(), callers.Current);
        return new NoteReadModel(notePriority.ToString());
    }

    /// <summary>
    /// Streams the notes, which a one-shot tool call cannot carry.
    /// </summary>
    /// <returns>The notes as an asynchronous stream.</returns>
    public static async IAsyncEnumerable<NoteReadModel> Stream()
    {
        yield return new NoteReadModel("streamed");
        await Task.CompletedTask;
    }
}
