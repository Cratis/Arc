// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Streaming queries that record invocation before returning a stream, not when it is enumerated.
/// </summary>
[ReadModel]
public record StreamingReadModel
{
    /// <summary>
    /// Returns an asynchronous stream after recording invocation.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The stream.</returns>
    public static IAsyncEnumerable<StreamingReadModel> Stream(McpProbe probe)
    {
        probe.Record("stream", null, null);
        return Rows();
    }

    /// <summary>
    /// Returns a subject after recording invocation.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The subject.</returns>
    public static ISubject<StreamingReadModel> Subject(McpProbe probe)
    {
        probe.Record("subject", null, null);
        return new Subject<StreamingReadModel>();
    }

    /// <summary>
    /// Returns a task-wrapped asynchronous stream.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The stream.</returns>
    public static Task<IAsyncEnumerable<StreamingReadModel>> StreamTask(McpProbe probe) => Task.FromResult(Stream(probe));

    /// <summary>
    /// Returns a task-wrapped subject.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The subject.</returns>
    public static Task<ISubject<StreamingReadModel>> SubjectTask(McpProbe probe) => Task.FromResult(Subject(probe));

    static async IAsyncEnumerable<StreamingReadModel> Rows()
    {
        yield return new StreamingReadModel();
        await Task.CompletedTask;
    }
}
