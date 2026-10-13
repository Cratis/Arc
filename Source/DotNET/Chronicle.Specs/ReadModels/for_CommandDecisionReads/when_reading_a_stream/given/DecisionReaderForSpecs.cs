// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream.given;

/// <summary>
/// Gives Castle invocation targets for Chronicle's default stream-scoped interface members.
/// </summary>
public abstract class DecisionReaderForSpecs : IDecisionReads
{
    public abstract DecisionReadAdmission Admit<T>()
        where T : class;
    public abstract DecisionReadAdmission AdmitForStream<T>()
        where T : class;
    public abstract Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class;
    public abstract Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class;
    public abstract Task<DecisionRead<T>> Get<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class;
    public abstract Task<DecisionRead<T>> GetDetached<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class;
}
