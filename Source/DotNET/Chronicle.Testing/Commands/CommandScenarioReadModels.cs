// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// An <see cref="IReadModels"/> for command scenarios that resolves a read model either from a pinned instance or by
/// projecting seeded events through the read model's own reducer or projection — keyed by event source id.
/// </summary>
/// <param name="inner">The inner <see cref="IReadModels"/> to delegate non-resolution operations to.</param>
/// <remarks>
/// Seeding events is deliberately type-agnostic: a test states the events that happened for an event source, and any
/// read model a command injects for that source is materialized from those events on demand — mirroring how the real
/// system derives read models from the event log. Pinned instances take precedence when a test wants to assert against
/// a specific read model value directly.
/// </remarks>
internal sealed class CommandScenarioReadModels(IReadModels inner) : IReadModels
{
    readonly Dictionary<(Type ReadModelType, string EventSourceId), object> _instances = [];
    readonly Dictionary<string, List<object>> _events = [];

    /// <inheritdoc/>
    public IMaterializedReadModels Materialized => inner.Materialized;

    /// <summary>
    /// Pins a materialized read model instance for an event source id.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="eventSourceId">The event source id to associate the instance with.</param>
    /// <param name="instance">The read model instance.</param>
    public void SeedInstance(Type readModelType, EventSourceId eventSourceId, object instance) =>
        _instances[(readModelType, eventSourceId.Value)] = instance;

    /// <summary>
    /// Seeds events for an event source id that read models are materialized from on demand.
    /// </summary>
    /// <param name="eventSourceId">The event source id the events happened for.</param>
    /// <param name="events">The events to seed.</param>
    public void SeedEvents(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        if (!_events.TryGetValue(eventSourceId.Value, out var seeded))
        {
            seeded = [];
            _events[eventSourceId.Value] = seeded;
        }

        seeded.AddRange(events);
    }

    /// <inheritdoc/>
    public async Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        if (_instances.TryGetValue((readModelType, key.Value), out var instance))
        {
            return instance;
        }

        if (_events.TryGetValue(key.Value, out var events))
        {
            return (await ProjectReadModel(readModelType, new EventSourceId(key.Value), events))!;
        }

        // Nothing seeded for this event source id: the read model does not exist. Command-scoped code can inject a
        // nullable read model and treat null as "does not exist", exactly as in production. We do not fall through to
        // the inner read models, which would require a live Chronicle backing store that a command scenario has not.
        return null!;
    }

    /// <inheritdoc/>
    public async Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null) =>
        (TReadModel)await GetInstanceById(typeof(TReadModel), key, sessionId);

    /// <inheritdoc/>
    public Task Register() => inner.Register();

    /// <inheritdoc/>
    public Task Register<TReadModel>() => inner.Register<TReadModel>();

    /// <inheritdoc/>
    public Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null) => inner.GetInstances<TReadModel>(eventCount);

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey) => inner.GetSnapshotsById<TReadModel>(readModelKey);

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>() => inner.Watch<TReadModel>();

    /// <inheritdoc/>
    public IReadModelWatcher<TReadModel> GetWatcherFor<TReadModel>() => inner.GetWatcherFor<TReadModel>();

    /// <inheritdoc/>
    public Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey) => inner.DehydrateSession(sessionId, readModelType, readModelKey);

    /// <inheritdoc/>
    public Task<TReadModel> Release<TReadModel>(TReadModel instance) => inner.Release(instance);

    /// <inheritdoc/>
    public Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances) => inner.Release(instances);

    static async Task<object?> ProjectReadModel(Type readModelType, EventSourceId eventSourceId, IEnumerable<object> events)
    {
        // ReadModelScenario<T> is Chronicle's own engine for materializing a read model from events through its real
        // reducer or projection — we build on it here rather than reimplementing projection. Reflection is used only
        // because it is generic and the read model type is known at runtime. We read the materialized Instance and
        // deliberately do NOT route through its ReadModels facade: that facade throws ("Read model returned null")
        // when a read model does not exist instead of returning null, which would break the nullable "does not exist"
        // semantics command-scoped code relies on.
        var scenarioType = typeof(ReadModelScenario<>).MakeGenericType(readModelType);
        var scenario = Activator.CreateInstance(scenarioType, CreateSeed(readModelType))!;

        var processTask = (Task)scenarioType.GetMethod("ProcessEventsFor")!.Invoke(scenario, [eventSourceId, events])!;
        await processTask;

        return scenarioType.GetProperty("Instance")!.GetValue(scenario);
    }

    static object CreateSeed(Type readModelType)
    {
        try
        {
            return Activator.CreateInstance(readModelType)!;
        }
        catch (MissingMethodException)
        {
            return RuntimeHelpers.GetUninitializedObject(readModelType);
        }
    }
}
