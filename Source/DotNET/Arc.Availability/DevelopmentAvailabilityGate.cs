// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Introspection;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents an <see cref="IAvailabilityGate"/> that lets <see cref="DevelopmentAvailability"/> switch whole modules, features
/// and slices on or off before the decorated gate decides.
/// </summary>
/// <param name="inner">The <see cref="IAvailabilityGate"/> that decides when nothing is switched.</param>
/// <param name="development">The <see cref="DevelopmentAvailability"/> holding the overrides.</param>
/// <param name="commands">The <see cref="ICommandHandlerProviders"/> that know every command, when the host has any.</param>
/// <param name="queries">The <see cref="IQueryPerformerProviders"/> that know every query, when the host has any.</param>
public class DevelopmentAvailabilityGate(
    IAvailabilityGate inner,
    DevelopmentAvailability development,
    ICommandHandlerProviders? commands,
    IQueryPerformerProviders? queries) : IAvailabilityGate
{
    /// <summary>
    /// The cause reported for a part that is switched off.
    /// </summary>
    public const string SwitchedOff = "Switched off by development tools";

    /// <inheritdoc/>
    public IReadOnlyCollection<Type> GatedParts
    {
        get
        {
            var disabled = development.Current.DisabledPaths;
            return disabled.Count == 0
                ? inner.GatedParts
                : [.. inner.GatedParts.Concat(AllParts().Where(part => IsWithinAny(part, disabled))).Distinct()];
        }
    }

    /// <inheritdoc/>
    public bool IsGated(Type part) =>
        inner.IsGated(part) || IsWithinAny(part, development.Current.DisabledPaths);

    /// <inheritdoc/>
    public ValueTask<AvailabilityDecision> Decide(Type part, IServiceProvider services, CancellationToken cancellationToken)
    {
        var overrides = development.Current;
        if (IsWithinAny(part, overrides.DisabledPaths))
        {
            return ValueTask.FromResult(new AvailabilityDecision(AvailabilityVerdict.Unavailable, [SwitchedOff]));
        }

        return IsWithinAny(part, overrides.EnabledPaths)
            ? ValueTask.FromResult(AvailabilityDecision.Available)
            : inner.Decide(part, services, cancellationToken);
    }

    /// <inheritdoc/>
    public IDisposable OnChanged(Func<ValueTask> handler)
    {
        var subscriptions = new[] { inner.OnChanged(handler), development.OnChanged(handler) };
        return new Subscriptions(subscriptions);
    }

    static bool IsWithinAny(Type part, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return false;
        }

        var segments = ApplicationStructureBuilder.SegmentsOf(part);
        return paths.Any(path => ApplicationStructureBuilder.IsWithin(segments, path));
    }

    IEnumerable<Type> AllParts() =>
        (commands?.Handlers.Select(_ => _.CommandType) ?? []).Concat(queries?.Performers.Select(_ => _.Type) ?? []);

    sealed class Subscriptions(IDisposable[] subscriptions) : IDisposable
    {
        public void Dispose()
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
