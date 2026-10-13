// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Json;

namespace Cratis.Arc.Chronicle.Testing.Commands;

internal sealed partial class CommandScenarioReadModels
{
    readonly BehaviorSubject<int> _pinsChanged = new(0);

    /// <summary>
    /// Gets whether seeds are being appended rather than command events.
    /// </summary>
    internal bool IsSeedingObservation { get; private set; }

    /// <inheritdoc/>
    public ISubject<IEnumerable<TReadModel>> Observe<TReadModel>(Expression<Func<TReadModel, bool>>? filter = null)
    {
        // Chronicle validates passive and unknown read models before the forwarding layer merges pins.
        var observation = inner.Observe<TReadModel>();
        ValidateObservationPins(typeof(TReadModel));
        var predicate = filter?.Compile();
        var source = observation.CombineLatest(_pinsChanged, (set, _) =>
        {
            var merged = MergeObservationPins(set);

            return (IEnumerable<TReadModel>)(predicate is null ? merged.ToArray() : merged.Where(predicate).ToArray());
        }).DistinctUntilChanged(set => JsonSerializer.Serialize(set, Globals.JsonSerializerOptions));

        return new ReadModelObservation<TReadModel>(source);
    }

    /// <inheritdoc/>
    public void Dispose() => _pinsChanged.Dispose();

    void ValidateObservationPins(Type readModelType)
    {
        if (_instances.Keys.Any(key => key.ReadModelType == readModelType) && ObservationReadModelKey.PropertyFor(readModelType) is null)
        {
            throw new PinnedReadModelCannotBeObserved(readModelType);
        }
    }

    IEnumerable<TReadModel> MergeObservationPins<TReadModel>(IEnumerable<TReadModel> set)
    {
        var pins = _instances.Where(pair => pair.Key.ReadModelType == typeof(TReadModel)).ToArray();
        if (pins.Length == 0)
        {
            return set;
        }

        var property = ObservationReadModelKey.PropertyFor(typeof(TReadModel)) ?? throw new PinnedReadModelCannotBeObserved(typeof(TReadModel));
        var keys = pins.Select(pin => pin.Key.EventSourceId).ToHashSet(StringComparer.Ordinal);

        return set.Where(instance => !keys.Contains(ObservationReadModelKey.Of(instance, property)))
            .Concat(pins.Select(pin => (TReadModel)pin.Value));
    }

    void SeedObservationEvents(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        if (observationLog is null)
        {
            return;
        }

        IsSeedingObservation = true;
        try
        {
            foreach (var @event in events)
            {
                var result = observationLog.Append(eventSourceId, @event).GetAwaiter().GetResult();
                if (!result.IsSuccess)
                {
                    throw new PriorEventCouldNotBeSeeded();
                }
            }
        }
        finally
        {
            IsSeedingObservation = false;
        }
    }
}
