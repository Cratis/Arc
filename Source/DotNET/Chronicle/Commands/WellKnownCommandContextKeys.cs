// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Well known keys for command context values.
/// </summary>
public static class WellKnownCommandContextKeys
{
    /// <summary>
    /// The key for the event source id in the command context values.
    /// </summary>
    public const string EventSourceId = "eventSourceId";

    /// <summary>
    /// The key for the event source type in the command context values.
    /// </summary>
    public const string EventSourceType = "eventSourceType";

    /// <summary>
    /// The key for the event source definition type in the command context values.
    /// </summary>
    public const string EventSource = "eventSource";

    /// <summary>
    /// The key for the event stream declared by the event source definition in the command context values.
    /// </summary>
    public const string EventStream = "eventStream";

    /// <summary>
    /// The key for the concurrency dimensions declared by the event source definition.
    /// </summary>
    public const string ConcurrencyDimensions = "concurrencyDimensions";

    /// <summary>
    /// The key for the event stream type in the command context values.
    /// </summary>
    public const string EventStreamType = "eventStreamType";

    /// <summary>
    /// The key for the event stream id in the command context values.
    /// </summary>
    public const string EventStreamId = "eventStreamId";

    /// <summary>
    /// The key for the subject in the command context values.
    /// </summary>
    public const string Subject = "subject";

    /// <summary>
    /// The key for named event tags returned by the command handler.
    /// </summary>
    public const string EventTags = "eventTags";
}
