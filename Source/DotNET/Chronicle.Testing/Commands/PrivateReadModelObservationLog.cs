// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Keeps observation-only history separate from the legacy command log and its constraints.
/// </summary>
internal sealed class PrivateReadModelObservationLog : IDisposable
{
    readonly object _gate = new();
    readonly Queue<EventForEventSourceId[]> _pending = new();
    readonly Subject<Unit> _failures = new();
    readonly IDisposable _appends;
    EventScenario? _scenario;
    Exception? _failure;
    bool _disposed;

    /// <summary>
    /// Captures successful command-log appends without creating another event scenario yet.
    /// </summary>
    /// <param name="commandLog">The command log to mirror.</param>
    public PrivateReadModelObservationLog(IEventSequence commandLog) =>
        _appends = commandLog.AppendOperations.Subscribe(Capture, Fail);

    /// <summary>
    /// Queues read-model seeds in occurrence order, without changing the command log.
    /// </summary>
    /// <param name="source">The event source.</param>
    /// <param name="events">The seeded events.</param>
    public void Seed(EventSourceId source, IEnumerable<object> events) =>
        Enqueue(events.Select(@event => new EventForEventSourceId(source, @event)).ToArray());

    /// <summary>
    /// Observes the private log through Chronicle's own testing read models.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <returns>The collection stream, including any failure to mirror history.</returns>
    public IObservable<IEnumerable<TReadModel>> Observe<TReadModel>()
    {
        lock (_gate)
        {
            _scenario ??= new EventScenario(EventSequenceId.Log, $"observation-{Guid.NewGuid():N}", "default", constraintProvider: null);
            var observation = _scenario.ReadModels.Observe<TReadModel>();
            Flush();
            if (_failure is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_failure).Throw();
            }

            return observation.Merge(_failures.Select(_ => Enumerable.Empty<TReadModel>()));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _appends.Dispose();
            _scenario?.Dispose();
            _failures.Dispose();
            _pending.Clear();
        }
    }

    static EventForEventSourceId ToInput(AppendedEvent appended)
    {
        var context = appended.Context;

        return new(context.EventSourceId, appended.Content)
        {
            EventSourceType = context.EventSourceType,
            EventStreamType = context.EventStreamType,
            EventStreamId = context.EventStreamId,
            Occurred = context.Occurred,
            Subject = context.Subject,
            Tags = context.Tags.Select(tag => tag.Value).ToArray(),
            NamedTags = context.NamedTags
        };
    }

    void Capture(IEnumerable<AppendedEventWithResult> appends) =>
        Enqueue(appends.Where(append => append.Result.IsSuccess).Select(append => ToInput(append.Event)).ToArray());

    void Enqueue(EventForEventSourceId[] events)
    {
        if (events.Length == 0)
        {
            return;
        }

        lock (_gate)
        {
            if (_disposed || _failure is not null)
            {
                return;
            }

            _pending.Enqueue(events);
            if (_scenario is not null)
            {
                Flush();
            }
        }
    }

    void Flush()
    {
        try
        {
            while (_failure is null && _pending.TryPeek(out var events))
            {
                var result = _scenario!.EventLog.AppendMany(events).GetAwaiter().GetResult();
                if (!result.IsSuccess)
                {
                    throw new PriorEventCouldNotBeSeeded();
                }
                _pending.Dequeue();
            }
        }
        catch (Exception error)
        {
            // The command append already succeeded. Fail observation permanently, not the command that committed it.
            Fail(error);
        }
    }

    void Fail(Exception error)
    {
        lock (_gate)
        {
            if (_disposed || _failure is not null)
            {
                return;
            }

            _failure = error;
            _failures.OnError(error);
        }
    }
}
