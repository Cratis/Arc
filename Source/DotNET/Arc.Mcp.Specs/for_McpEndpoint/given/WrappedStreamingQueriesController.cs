// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Controller query shapes whose invocation is recorded before a stream can be consumed.
/// </summary>
public class WrappedStreamingQueriesController : ControllerBase
{
    /// <summary>
    /// Returns a value-task-wrapped stream.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The stream.</returns>
    public ValueTask<IAsyncEnumerable<StreamingReadModel>> StreamValueTask(McpProbe probe) => ValueTask.FromResult(StreamingReadModel.Stream(probe));

    /// <summary>
    /// Returns a value-task-wrapped subject.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The subject.</returns>
    public ValueTask<ISubject<StreamingReadModel>> SubjectValueTask(McpProbe probe) => ValueTask.FromResult(StreamingReadModel.Subject(probe));

    /// <summary>
    /// Returns an MVC-wrapped stream.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The stream.</returns>
    public ActionResult<IAsyncEnumerable<StreamingReadModel>> StreamResult(McpProbe probe) => new(StreamingReadModel.Stream(probe));

    /// <summary>
    /// Returns an MVC-wrapped subject.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The subject.</returns>
    public ActionResult<ISubject<StreamingReadModel>> SubjectResult(McpProbe probe) => new(StreamingReadModel.Subject(probe));

    /// <summary>
    /// Returns a task- and MVC-wrapped stream.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The stream.</returns>
    public Task<ActionResult<IAsyncEnumerable<StreamingReadModel>>> StreamResultTask(McpProbe probe) => Task.FromResult(StreamResult(probe));

    /// <summary>
    /// Returns a task- and MVC-wrapped subject.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The subject.</returns>
    public Task<ActionResult<ISubject<StreamingReadModel>>> SubjectResultTask(McpProbe probe) => Task.FromResult(SubjectResult(probe));

    /// <summary>
    /// Returns a value-task- and MVC-wrapped stream.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The stream.</returns>
    public ValueTask<ActionResult<IAsyncEnumerable<StreamingReadModel>>> StreamResultValueTask(McpProbe probe) => ValueTask.FromResult(StreamResult(probe));

    /// <summary>
    /// Returns a value-task- and MVC-wrapped subject.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The subject.</returns>
    public ValueTask<ActionResult<ISubject<StreamingReadModel>>> SubjectResultValueTask(McpProbe probe) => ValueTask.FromResult(SubjectResult(probe));

    /// <summary>
    /// Returns an ordinary MVC-wrapped read model.
    /// </summary>
    /// <param name="probe">The invocation probe.</param>
    /// <returns>The read model.</returns>
    public ActionResult<NoteReadModel> Snapshot(McpProbe probe)
    {
        probe.Record("snapshot", null, null);
        return new NoteReadModel("snapshot");
    }
}
