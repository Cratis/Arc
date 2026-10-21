// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Owns immutable whole-source guards and their provenance for one command invocation.
/// </summary>
/// <param name="captures">The invocation's live generated-value captures.</param>
internal sealed class FreshIdentifierScopes(GeneratedValues captures)
{
    static readonly ConditionalWeakTable<CommandContextValues, FreshIdentifierScopes> _invocations = new();
    static readonly ConditionalWeakTable<IUnitOfWork, FreshIdentifierProvenance> _units = new();
    readonly Dictionary<(EventSequenceId Sequence, EventSourceId Source), ConcurrencyScope> _scopes = [];

    /// <summary>
    /// Gets the registry shared by all copies of the invocation context.
    /// </summary>
    /// <param name="context">The invocation.</param>
    /// <returns>Its registry.</returns>
    internal static FreshIdentifierScopes For(CommandContext context) =>
        _invocations.GetValue(context.Values, _ => new(context.GeneratedValues));

    /// <summary>
    /// Gets the freshness provenance enrolled in a unit of work.
    /// </summary>
    /// <param name="unit">The unit of work.</param>
    /// <returns>Its provenance, or null when no generated guard was enrolled.</returns>
    internal static FreshIdentifierProvenance? For(IUnitOfWork unit) =>
        _units.TryGetValue(unit, out var provenance) ? provenance : null;

    /// <summary>
    /// Checks whether a scope represents the immutable whole-source freshness expectation.
    /// </summary>
    /// <param name="scope">The scope to classify.</param>
    /// <param name="source">The append target.</param>
    /// <returns>Whether the scope requires whole-source emptiness.</returns>
    internal static bool IsWholeSourceBeforeFirst(ConcurrencyScope scope, EventSourceId source) =>
        scope.SequenceNumber.IsBeforeFirst && scope.EventSourceId == source &&
        (scope.EventStreamType is null || scope.EventStreamType == EventStreamType.All) &&
        (scope.EventStreamId is null || scope.EventStreamId == EventStreamId.Default) &&
        (scope.EventSourceType is null || scope.EventSourceType == EventSourceType.Default || scope.EventSourceType == EventSourceType.Unspecified) &&
        scope.EventTypes?.Any() != true;

    /// <summary>
    /// Matches a real append target against guarded captures, without generating another value.
    /// </summary>
    /// <param name="source">The append target.</param>
    /// <returns>Whether any guarded capture matches.</returns>
    internal bool Matches(EventSourceId source) => MatchesOwnCapture(source) ||
        (CommandTransaction.TryGetActive(out var unit) && TransactionalCommandScope.MatchesFreshIdentifier(unit, source));

    /// <summary>
    /// Matches only this invocation's captures, without traversing the active invocation chain.
    /// </summary>
    /// <param name="source">The append target.</param>
    /// <returns>Whether this invocation captured the target with a guarded policy.</returns>
    internal bool MatchesOwnCapture(EventSourceId source) => captures.Any(capture =>
        capture.Freshness == GeneratedValueFreshness.Guarded &&
        string.Equals(GeneratedIdentifierCanonicalizer.Canonicalize(capture.Value), source.Value, StringComparison.Ordinal));

    /// <summary>
    /// Composes a caller intent with the immutable generated-target guard.
    /// </summary>
    /// <param name="sequence">The destination sequence.</param>
    /// <param name="source">The destination source.</param>
    /// <param name="scope">The caller's scope.</param>
    /// <param name="explicitScope">Whether the caller supplied an exact scope in EventsWithConcurrencyScopes, rather than a scope derived from routing or concurrency attributes.</param>
    /// <returns>The composed scope.</returns>
    /// <exception cref="ConflictingConcurrencyScopes">An explicit intent differs from whole-source emptiness.</exception>
    internal ConcurrencyScope? Resolve(EventSequenceId sequence, EventSourceId source, ConcurrencyScope? scope, bool explicitScope)
    {
        if (!RequiresGuard(sequence, source))
        {
            return scope;
        }

        if (explicitScope && scope is not null && scope != ConcurrencyScope.NotSet && !IsWholeSourceBeforeFirst(scope, source))
        {
            throw new ConflictingConcurrencyScopes();
        }

        var key = (sequence, source);
        if (!_scopes.TryGetValue(key, out var guarded))
        {
            guarded = new(EventSequenceNumber.BeforeFirst, EventSourceId: source);
            _scopes.Add(key, guarded);
        }

        return guarded;
    }

    /// <summary>
    /// Records provenance after successful enrollment, including nested commands sharing a unit.
    /// </summary>
    /// <param name="unit">The receiving unit.</param>
    /// <param name="sequence">The destination sequence.</param>
    /// <param name="source">The destination source.</param>
    internal void Enrolled(IUnitOfWork unit, EventSequenceId sequence, EventSourceId source)
    {
        if (_scopes.ContainsKey((sequence, source)))
        {
            _units.GetValue(unit, _ => new()).Add(sequence, source);
        }
    }

    /// <summary>
    /// Checks an immediate failure's provenance in its known sequence.
    /// </summary>
    /// <param name="sequence">The failed append's sequence.</param>
    /// <param name="violation">The reported violation.</param>
    /// <returns>Whether it came from this invocation's freshness guard.</returns>
    internal bool Owns(EventSequenceId sequence, ConcurrencyViolation violation) =>
        violation.ExpectedEventSequenceNumber.IsBeforeFirst && _scopes.ContainsKey((sequence, violation.EventSourceId));

    /// <summary>
    /// Validates every batch intent before enrollment and merges equal generated-target guards.
    /// </summary>
    /// <param name="sequence">The destination sequence.</param>
    /// <param name="events">The batch of actual targets.</param>
    /// <param name="scopes">Explicit scopes, including independent labels.</param>
    /// <returns>The composed batch scopes.</returns>
    internal KeyValuePair<EventSourceId, ConcurrencyScope>[] ResolveBatch(
        EventSequenceId sequence,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> scopes)
    {
        var supplied = scopes.ToArray();
        var targets = events.Select(@event => @event.EventSourceId).Distinct().Where(source => RequiresGuard(sequence, source)).ToArray();
        foreach (var target in targets)
        {
            // Check every intent, not just the last dictionary entry, before any event can be enrolled.
            foreach (var intent in supplied.Where(intent => intent.Key == target || intent.Value.EventSourceId == target))
            {
                Resolve(sequence, target, intent.Value, true);
            }
        }

        return supplied.Where(intent => !targets.Contains(intent.Key))
            .Concat(targets.Select(target => new KeyValuePair<EventSourceId, ConcurrencyScope>(target, Resolve(sequence, target, null, false)!)))
            .ToArray();
    }

    /// <summary>
    /// Checks whether this invocation resolved a freshness guard for the target.
    /// </summary>
    /// <param name="sequence">The sequence.</param>
    /// <param name="source">The source.</param>
    /// <returns>Whether the target has a freshness guard.</returns>
    internal bool HasGuard(EventSequenceId sequence, EventSourceId source) => _scopes.ContainsKey((sequence, source));

    bool RequiresGuard(EventSequenceId sequence, EventSourceId source) => Matches(source) ||
        (CommandTransaction.TryGetActive(out var unit) && For(unit)?.Contains(sequence, source) == true);
}
