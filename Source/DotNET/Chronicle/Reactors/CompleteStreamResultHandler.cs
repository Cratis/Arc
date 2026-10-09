// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.DependencyInjection;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Completes a stream declared by a reactor, tolerating redelivery of the completion.
/// </summary>
[Singleton]
public class CompleteStreamResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) => value is CompleteStream;

    /// <inheritdoc/>
    public bool CanHandleReturnType(Type type) => type == typeof(CompleteStream);

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var completion = (CompleteStream)value;
        var streamType = completion.EventStreamType ?? reactorContext.EventContext.EventStreamType;
        var streamId = completion.EventStreamId ?? reactorContext.EventContext.EventStreamId;
        if (streamId.IsDefault || streamId == EventStreamId.NotSet)
        {
            return Failure("The default stream cannot be completed.");
        }

        var result = await eventStore.EventLog.CompleteStream(streamType, streamId);
        if (!result.IsSuccess && (!result.TryGetError(out var error) || error != CompleteStreamError.AlreadyCompleted))
        {
            return Failure($"Stream '{streamType}/{streamId}' could not be completed: {error}.");
        }

        return Result.Success<ReactorSideEffectFailure>();
    }

    static Result<ReactorSideEffectFailure> Failure(string message) =>
        Result.Failed(new ReactorSideEffectFailure([new AppendFailure([], false, [message], [])]));
}
