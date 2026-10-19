// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Resolves the guards a command's events carry for each event source id they target.
/// </summary>
/// <remarks>
/// The caller's own choice covers every event of its id. Otherwise each distinct legacy-stream guard the events imply is
/// kept, in event order, and Chronicle requires all of them (a conjunction); guards for the same predicate collapse to one.
/// Guards derived from an event source definition are never passed: Chronicle skips deriving them for an id once it is
/// handed a scope, so a legacy guard sent alongside would quietly leave the definition's events unguarded. That
/// combination fails here, before anything is enrolled or appended.
/// </remarks>
internal static class CommandConcurrencyScopes
{
    /// <summary>
    /// Resolve the scopes to attach for each event source id.
    /// </summary>
    /// <param name="entries">The derived guard of every event, in order.</param>
    /// <returns>The scopes for each event source id, in event order; ids that need none map to an empty list.</returns>
    /// <exception cref="IncompatibleConcurrencyScopesForEventSource">Events for one id need legacy guards and also have a guard Chronicle derives from the event source definition.</exception>
    internal static IReadOnlyDictionary<EventSourceId, IReadOnlyList<ConcurrencyScope>> Resolve(IEnumerable<(EventSourceId EventSourceId, DerivedConcurrencyScope Derived)> entries)
    {
        var result = new Dictionary<EventSourceId, IReadOnlyList<ConcurrencyScope>>();
        foreach (var group in entries.GroupBy(_ => _.EventSourceId))
        {
            var derived = group.Select(_ => _.Derived).ToList();

            // The caller's own choice keeps its authority over every event of the id, as it always has.
            var explicitScope = derived.Find(_ => _.Origin == DerivedConcurrencyScopeOrigin.Explicit);
            if (explicitScope is not null)
            {
                result[group.Key] = explicitScope.Scope is null ? [] : [explicitScope.Scope];
                continue;
            }

            var distinct = new List<ConcurrencyScope>();
            derived
                .Where(_ => _.Origin == DerivedConcurrencyScopeOrigin.Implicit && ChecksSomething(_.Scope!))
                .Select(_ => _.Scope!)
                .ToList()
                .ForEach(guard =>
                {
                    if (!distinct.Exists(_ => SamePredicate(_, guard)))
                    {
                        distinct.Add(guard);
                    }
                });

            // A guard attached to the id is explicit to the unit of work, so it would also stand in for the events the
            // event sequence would have guarded from their own definition.
            if (distinct.Count > 0 && derived.Exists(_ => _.Origin == DerivedConcurrencyScopeOrigin.DerivedByEventSequence))
            {
                throw new IncompatibleConcurrencyScopesForEventSource(group.Key, distinct);
            }

            result[group.Key] = distinct;
        }

        return result;
    }

    /// <summary>
    /// Gets the scope to enroll an event with: its own guard when the id carries several, otherwise the id's only one.
    /// </summary>
    /// <param name="guards">The guards resolved for the event's event source id.</param>
    /// <param name="derived">The guard derived for the event itself.</param>
    /// <returns>The scope to enroll the event with, or null when the id needs none.</returns>
    internal static ConcurrencyScope? EnrollmentScopeFor(IReadOnlyList<ConcurrencyScope> guards, DerivedConcurrencyScope derived) =>
        guards.Count > 1 && derived.Scope is not null
            ? guards.FirstOrDefault(_ => SamePredicate(_, derived.Scope)) ?? guards[0]
            : guards.Count > 0 ? guards[0] : null;

    static bool ChecksSomething(ConcurrencyScope scope) =>
        scope != ConcurrencyScope.NotSet && scope != ConcurrencyScope.None && (scope.SequenceNumber.IsActualValue || scope.SequenceNumber.IsBeforeFirst);

    static bool SamePredicate(ConcurrencyScope left, ConcurrencyScope right) =>
        left.EventSourceId == right.EventSourceId &&
        left.EventSourceType == right.EventSourceType &&
        left.EventStreamType == right.EventStreamType &&
        left.EventStreamId == right.EventStreamId &&
        (left.EventTypes ?? []).ToHashSet().SetEquals(right.EventTypes ?? []);
}
