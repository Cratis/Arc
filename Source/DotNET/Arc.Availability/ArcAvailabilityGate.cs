// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;
using Cratis.Arc.Introspection;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // The string-based attributes are obsolete, and still honored until they are removed.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the <see cref="IAvailabilityGate"/> that decides with the availability engine.
/// </summary>
/// <param name="options">The <see cref="ArcAvailabilityOptions"/>.</param>
/// <param name="evaluator">The <see cref="IAvailabilityEvaluator"/>.</param>
/// <param name="audienceSource">The <see cref="IAudienceSource"/> whose changes are announced.</param>
/// <param name="types">The <see cref="ITypes"/> used to find the parts that declare a gate with attributes.</param>
/// <param name="services">The services that create the conditions that are classes and inject what they need.</param>
/// <param name="suspensions">The <see cref="ISuspensionSource"/> whose decisions are checked first. When there is none, nothing can be suspended.</param>
/// <param name="development">The <see cref="DevelopmentAvailability"/> that lets a developer switch parts on or off while the application runs. When there is none, nothing is switched.</param>
[IgnoreConvention]
public class ArcAvailabilityGate(ArcAvailabilityOptions options, IAvailabilityEvaluator evaluator, IAudienceSource audienceSource, ITypes types, IServiceProvider? services = null, ISuspensionSource? suspensions = null, DevelopmentAvailability? development = null) : IAvailabilityExecutionGate
{
    /// <summary>
    /// The cause reported for a part that a developer switched off.
    /// </summary>
    public const string SwitchedOff = "Switched off by development tools";

    readonly Func<Type, IAvailabilityCondition> _conditions = TypedAvailability.ConditionFactory(services);
    readonly Lazy<IReadOnlyList<(string Namespace, AvailabilityExpression Gate)>> _markers = new(() => TypedAvailability.Markers(types, TypedAvailability.ConditionFactory(services)));
    readonly ConcurrentDictionary<Type, IReadOnlyList<AvailabilityExpression>> _paths = new();
    readonly Lazy<IReadOnlyCollection<Type>> _gatedParts = new(() => FindGatedParts(options, types));
    readonly Lazy<IReadOnlyList<Type>> _discovered = new(() => new DiscoveredParts(types).All);

    /// <inheritdoc/>
    public IReadOnlyCollection<Type> GatedParts
    {
        get
        {
            var disabled = development?.Current.DisabledPaths ?? [];
            return disabled.Count == 0
                ? _gatedParts.Value
                : [.. _gatedParts.Value.Concat(_discovered.Value.Where(part => IsWithinAny(part, disabled))).Distinct()];
        }
    }

    /// <summary>
    /// Gets the gates of the namespaces, which are the modules, features and slices that carry one.
    /// </summary>
    public IReadOnlyList<(string Namespace, AvailabilityExpression Gate)> Scopes =>
        [.. options.Namespaces.Select(_ => (_.Namespace, _.Gate)).Concat(_markers.Value)];

    /// <summary>
    /// Gets the gates down the path of a part, outermost first. Every one must hold.
    /// </summary>
    /// <param name="part">The command type, or the type declaring the query.</param>
    /// <returns>The gates; none when nothing gates the part.</returns>
    public IReadOnlyList<AvailabilityExpression> GatesOf(Type part) => PathOf(part);

    /// <inheritdoc/>
    public bool IsGated(Type part) => suspensions is not null || PathOf(part).Count > 0 || Switch(part) != DevelopmentSwitch.None;

    /// <inheritdoc/>
    public ValueTask<AvailabilityDecision> Decide(Type part, IServiceProvider services, CancellationToken cancellationToken) =>
        IsGated(part)
            ? Decide(part, services.GetRequiredService<AvailabilityRequestContext>(), null, cancellationToken)
            : ValueTask.FromResult(AvailabilityDecision.Available);

    /// <inheritdoc/>
    public ValueTask<AvailabilityDecision> Decide(AvailabilityInvocation invocation, IServiceProvider services, CancellationToken cancellationToken) =>
        IsGated(invocation.Part)
            ? Decide(invocation.Part, services.GetRequiredService<AvailabilityRequestContext>(), invocation, cancellationToken)
            : ValueTask.FromResult(AvailabilityDecision.Available);

    /// <inheritdoc/>
    public IDisposable OnChanged(Func<ValueTask> handler)
    {
        var audiences = audienceSource.OnChanged(_ => handler());
        var suspended = suspensions?.OnChanged(_ => handler());
        var switched = development?.OnChanged(handler);
        return new Subscription(() =>
        {
            audiences.Dispose();
            suspended?.Dispose();
            switched?.Dispose();
        });
    }

    static IReadOnlyCollection<Type> FindGatedParts(ArcAvailabilityOptions options, ITypes types)
    {
        var declared = types.All.Where(type =>
            (type.IsClass || type.IsValueType) &&
            (type.IsDefined(typeof(AvailableForAttribute), false) || type.IsDefined(typeof(AvailableWithAttribute), false) || HasTypedGate(type)));
        var registered = options.Parts.Select(_ => _.Part);
        var markers = TypedAvailability.MarkerNamespaces(types);
        var inNamespaces = types.All.Where(type => type.Namespace is not null && (options.Namespaces.Any(_ => InNamespace(type, _.Namespace)) || markers.Any(_ => InNamespace(type, _))));
        return [.. declared.Concat(registered).Concat(inNamespaces).Distinct()];
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

    static bool HasTypedGate(Type type) => type.GetCustomAttributes(false).OfType<IAvailabilityAttribute>().Any();

    static bool InNamespace(Type type, string @namespace) =>
        type.Namespace is { } typeNamespace && (typeNamespace == @namespace || typeNamespace.StartsWith(@namespace + ".", StringComparison.Ordinal));

    DevelopmentSwitch Switch(Type part)
    {
        var overrides = development?.Current;
        if (overrides?.IsEmpty != false)
        {
            return DevelopmentSwitch.None;
        }

        if (IsWithinAny(part, overrides.DisabledPaths))
        {
            return DevelopmentSwitch.Off;
        }

        return IsWithinAny(part, overrides.EnabledPaths) ? DevelopmentSwitch.On : DevelopmentSwitch.None;
    }

    async ValueTask<AvailabilityDecision> Decide(Type part, AvailabilityRequestContext request, AvailabilityInvocation? invocation, CancellationToken cancellationToken)
    {
        var context = request;
        try
        {
            // What a developer switched is decided before anything else, whatever the gates and suspensions say.
            switch (Switch(part))
            {
                case DevelopmentSwitch.Off:
                    return new AvailabilityDecision(AvailabilityVerdict.Unavailable, [SwitchedOff]);
                case DevelopmentSwitch.On:
                    return AvailabilityDecision.Available;
            }

            // A suspension is decided first: before any gate, so before the audiences and the context values are resolved.
            if (suspensions is not null && (await request.Suspended(suspensions, cancellationToken)).Covers(part))
            {
                return new AvailabilityDecision(AvailabilityVerdict.Suspended, [AvailabilityReason.Suspended]);
            }

            if (PathOf(part).Count == 0)
            {
                return AvailabilityDecision.Available;
            }

            context = invocation is null ? request : request.ForInvocation(invocation);
            return await Evaluate(part, context, cancellationToken);
        }
        catch
        {
            request.Reset();
            context.Reset();
            throw;
        }
    }

    async ValueTask<AvailabilityDecision> Evaluate(Type part, AvailabilityRequestContext request, CancellationToken cancellationToken)
    {
        var path = PathOf(part);
        if (path.Count == 0)
        {
            return AvailabilityDecision.Available;
        }

        var resolver = request.ResolverFor(options.ScopeFor(part));
        var result = await evaluator.EvaluatePath(path, resolver, cancellationToken);
        return result.Outcome switch
        {
            AvailabilityOutcome.Available => AvailabilityDecision.Available,
            AvailabilityOutcome.Unavailable => new AvailabilityDecision(AvailabilityVerdict.Unavailable, result.Causes),
            _ => new AvailabilityDecision(AvailabilityVerdict.Unknown, result.Causes)
        };
    }

    IReadOnlyList<AvailabilityExpression> PathOf(Type part) => _paths.GetOrAdd(part, BuildPath);

    List<AvailabilityExpression> BuildPath(Type part)
    {
        var path = new List<AvailabilityExpression>();
        path.AddRange(options.Namespaces
            .Where(_ => InNamespace(part, _.Namespace))
            .OrderBy(_ => _.Namespace.Length)
            .Select(_ => _.Gate));
        path.AddRange(_markers.Value
            .Where(_ => InNamespace(part, _.Namespace))
            .OrderBy(_ => _.Namespace.Length)
            .Select(_ => _.Gate));
        path.AddRange(options.Parts.Where(_ => _.Part == part).Select(_ => _.Gate));
        path.AddRange(TypedAvailability.Gates(part, _conditions));
        foreach (var attribute in part.GetCustomAttributes(false))
        {
            switch (attribute)
            {
                case AvailableForAttribute availableFor:
                    path.Add(AvailabilityExpression.For([.. availableFor.Audiences.Select(audience => new AudienceName(audience))]));
                    break;
                case AvailableWithAttribute availableWith:
                    path.Add(AvailabilityExpression.With(availableWith.Name));
                    break;
            }
        }

        return path;
    }

    sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
