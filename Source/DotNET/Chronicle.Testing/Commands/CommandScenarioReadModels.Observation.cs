// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Json;

namespace Cratis.Arc.Chronicle.Testing.Commands;

internal sealed partial class CommandScenarioReadModels
{
    readonly BehaviorSubject<int> _pinsChanged = new(0);

    /// <inheritdoc/>
    public ISubject<IEnumerable<TReadModel>> Observe<TReadModel>(Expression<Func<TReadModel, bool>>? filter = null)
    {
        // Chronicle validates passive and unknown read models before the forwarding layer merges pins.
        var observation = _observationLog is null ? inner.Observe<TReadModel>() : _observationLog.Observe<TReadModel>();
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
    public void Dispose()
    {
        _observationLog?.Dispose();
        _pinsChanged.Dispose();
    }

    void ValidateObservationPins(Type readModelType)
    {
        var pins = _instances.Where(pair => pair.Key.ReadModelType == readModelType).Select(pair => pair.Value).ToArray();
        if (pins.Length == 0)
        {
            return;
        }

        var property = ObservationReadModelKey.PropertyFor(readModelType) ?? throw new PinnedReadModelCannotBeObserved(readModelType);
        foreach (var pin in pins)
        {
            ObservationReadModelKey.Of(pin, property);
        }
    }

    IEnumerable<TReadModel> MergeObservationPins<TReadModel>(IEnumerable<TReadModel> set)
    {
        var pins = _instances.Where(pair => pair.Key.ReadModelType == typeof(TReadModel)).ToArray();
        if (pins.Length == 0)
        {
            return set;
        }

        ValidateObservationPins(typeof(TReadModel));
        var property = ObservationReadModelKey.PropertyFor(typeof(TReadModel))!;
        var keys = pins.Select(pin => pin.Key.EventSourceId).ToHashSet(StringComparer.Ordinal);

        return set.Where(instance => !keys.Contains(ObservationReadModelKey.Of(instance, property)))
            .Concat(pins.Select(pin => (TReadModel)pin.Value));
    }
}
