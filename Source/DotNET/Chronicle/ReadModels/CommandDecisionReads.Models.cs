// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

internal sealed partial class CommandDecisionReads
{
    /// <summary>
    /// Resolves and releases a plain model once, sharing the instance with validators, Provide and Handle.
    /// </summary>
    /// <typeparam name="T">The read model.</typeparam>
    /// <param name="key">The resolved key.</param>
    /// <param name="cancellationToken">The invocation cancellation.</param>
    /// <returns>The released model, or null for an absent instance.</returns>
    internal async Task<T?> GetInstance<T>(ReadModelKey key, CancellationToken cancellationToken)
        where T : class
    {
        var read = await Get<T>(key, cancellationToken);
        return await ReleaseInstance(read);
    }

    /// <summary>
    /// Resolves a plain per-stream model through the invocation's enrolled stream-read cache.
    /// </summary>
    /// <typeparam name="T">The model.</typeparam>
    /// <param name="source">The owning source.</param>
    /// <param name="streamType">The route's stream type.</param>
    /// <param name="streamId">The route's stream id.</param>
    /// <param name="sourceType">The route's source type.</param>
    /// <param name="cancellationToken">The invocation cancellation.</param>
    /// <returns>The released stream model.</returns>
    /// <exception cref="DecisionReadRefused">The model cannot be protected for this stream.</exception>
    internal async Task<T?> GetStreamInstance<T>(EventSourceId source, EventStreamType streamType, EventStreamId streamId, EventSourceType sourceType, CancellationToken cancellationToken)
        where T : class
    {
        var description = Describe<T>();
        var admission = AdmitForStream<T>();
        if (!admission.IsAdmitted || description.UsesPinnedGenerationDelivery)
        {
            throw new DecisionReadRefused(description.UsesPinnedGenerationDelivery ? DecisionReadRefusalReason.GenerationDeliveryNotSupported : admission.Reason!.Value, typeof(T));
        }
        return await ReleaseInstance(await Get<T>(source, streamType, streamId, sourceType, cancellationToken));
    }

    async Task<T?> ReleaseInstance<T>(DecisionRead<T> read)
        where T : class
    {
        var invocation = InvocationForCurrentPolicy()!;
        async Task<object?> Release() => read.Instance is null ? null : await readModels.Release(read.Instance);
        var unit = CommandValidationExecution.Current is null ? UnitOfWorkFor(ReadMode.Protected) : null;
        var instance = CacheFor(invocation, unit).Instances.GetOrAdd(read, static (_, release) => new Lazy<Task<object?>>(release, LazyThreadSafetyMode.ExecutionAndPublication), Release);

        return (T?)await instance.Value;
    }
}
