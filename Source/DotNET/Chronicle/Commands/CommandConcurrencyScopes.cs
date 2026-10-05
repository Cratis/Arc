// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Resolves the single guard a command's events carry for each event source id they target.
/// </summary>
/// <remarks>
/// A unit of work carries one scope per event source id and, for a scope it was handed, keeps the first one without
/// comparing it to the others. Guards Arc derives from an event source definition are therefore compared here, before
/// anything is enrolled or appended: two events needing different guards for one id fail instead of the later event
/// quietly running unguarded.
/// </remarks>
internal static class CommandConcurrencyScopes
{
    /// <summary>
    /// Resolve the scope to attach for each event source id.
    /// </summary>
    /// <param name="entries">The derived guard of every event, in order.</param>
    /// <returns>The scope for each event source id; ids that need none map to null.</returns>
    /// <exception cref="IncompatibleConcurrencyScopesForEventSource">Events for one id need different guards.</exception>
    internal static IReadOnlyDictionary<EventSourceId, ConcurrencyScope?> Resolve(IEnumerable<(EventSourceId EventSourceId, DerivedConcurrencyScope Derived)> entries)
    {
        var result = new Dictionary<EventSourceId, ConcurrencyScope?>();
        foreach (var group in entries.GroupBy(_ => _.EventSourceId))
        {
            var derived = group.Select(_ => _.Derived).ToList();

            // The caller's own choice keeps its authority over every event of the id, as it always has.
            var explicitScope = derived.Find(_ => _.Origin == DerivedConcurrencyScopeOrigin.Explicit);
            if (explicitScope is not null)
            {
                result[group.Key] = explicitScope.Scope;
                continue;
            }

            var guards = derived.Where(_ => _.Origin == DerivedConcurrencyScopeOrigin.Implicit && ChecksSomething(_.Scope!)).Select(_ => _.Scope!).ToList();
            var distinct = new List<ConcurrencyScope>();
            guards.ForEach(guard =>
            {
                if (!distinct.Exists(_ => SamePredicate(_, guard)))
                {
                    distinct.Add(guard);
                }
            });

            // A guard attached to the id is explicit to the unit of work, so it would also stand in for the events the
            // event sequence would have guarded from their own definition.
            var needsMore = distinct.Count > 1 || (distinct.Count == 1 && derived.Exists(_ => _.Origin == DerivedConcurrencyScopeOrigin.DerivedByEventSequence));
            result[group.Key] = needsMore
                ? throw new IncompatibleConcurrencyScopesForEventSource(group.Key, distinct)
                : guards.FirstOrDefault();
        }

        return result;
    }

    static bool ChecksSomething(ConcurrencyScope scope) =>
        scope != ConcurrencyScope.NotSet && scope != ConcurrencyScope.None && (scope.SequenceNumber.IsActualValue || scope.SequenceNumber.IsBeforeFirst);

    static bool SamePredicate(ConcurrencyScope left, ConcurrencyScope right) =>
        left.EventSourceId == right.EventSourceId &&
        left.EventSourceType == right.EventSourceType &&
        left.EventStreamType == right.EventStreamType &&
        left.EventStreamId == right.EventStreamId &&
        (left.EventTypes ?? []).ToHashSet().SetEquals(right.EventTypes ?? []);
}
