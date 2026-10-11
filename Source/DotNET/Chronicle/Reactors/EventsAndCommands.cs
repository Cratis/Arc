// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// A validated, invocation-local partition of mixed reactor side effects.
/// </summary>
/// <param name="Events">The events in their original relative order.</param>
/// <param name="Commands">The commands in their original relative order.</param>
internal record EventsAndCommands(IEnumerable<object> Events, IEnumerable<object> Commands)
{
    enum EffectKind
    {
        Invalid = 0,
        Event = 1,
        Command = 2
    }

    /// <summary>
    /// Materializes and validates the sequence before any side effect begins.
    /// </summary>
    /// <param name="eventTypes">The current event store's registry.</param>
    /// <param name="value">The reactor result.</param>
    /// <param name="effects">The validated partition, if accepted.</param>
    /// <returns>Whether the sequence contains only valid events and commands, including at least one of each.</returns>
    public static bool TryCreate(IEventTypes eventTypes, object value, [NotNullWhen(true)] out EventsAndCommands? effects)
    {
        effects = null;
        if (value is not IEnumerable<object> sequence)
        {
            return false;
        }

        var items = sequence.Select(item => (Value: item, Kind: Classify(eventTypes, item))).ToArray();
        if (!items.Select(item => item.Kind).ToHashSet().SetEquals([EffectKind.Event, EffectKind.Command]))
        {
            return false;
        }

        effects = new(
            items.Where(item => item.Kind == EffectKind.Event).Select(item => item.Value).ToArray(),
            items.Where(item => item.Kind == EffectKind.Command).Select(item => item.Value).ToArray());

        return true;
    }

    static EffectKind Classify(IEventTypes eventTypes, object? value)
    {
        if (value is null or CompleteStream)
        {
            return EffectKind.Invalid;
        }

        if (value is EventForEventSourceId envelope)
        {
            return IsEvent(eventTypes, envelope.Event) ? EffectKind.Event : EffectKind.Invalid;
        }

        var isEvent = eventTypes.HasFor(value.GetType());
        var isCommand = value.GetType().IsCommand();

        return (isEvent, isCommand) switch
        {
            (true, false) => EffectKind.Event,
            (false, true) => EffectKind.Command,
            _ => EffectKind.Invalid
        };
    }

    static bool IsEvent(IEventTypes eventTypes, object? value) =>
        value is not null and not CompleteStream and not EventForEventSourceId &&
        eventTypes.HasFor(value.GetType()) && !value.GetType().IsCommand();
}
