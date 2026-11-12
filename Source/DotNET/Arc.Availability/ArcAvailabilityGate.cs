// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Choreography.Availability;
using Cratis.Choreography.Availability.Audiences;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the <see cref="IAvailabilityGate"/> that decides with the Choreography availability engine.
/// </summary>
/// <param name="options">The <see cref="ArcAvailabilityOptions"/>.</param>
/// <param name="evaluator">The <see cref="IAvailabilityEvaluator"/>.</param>
/// <param name="audienceSource">The <see cref="IAudienceSource"/> whose changes are announced.</param>
/// <param name="types">The <see cref="ITypes"/> used to find the parts that declare a gate with attributes.</param>
public class ArcAvailabilityGate(ArcAvailabilityOptions options, IAvailabilityEvaluator evaluator, IAudienceSource audienceSource, ITypes types) : IAvailabilityGate
{
    readonly ConcurrentDictionary<Type, IReadOnlyList<AvailabilityExpression>> _paths = new();
    readonly Lazy<IReadOnlyCollection<Type>> _gatedParts = new(() => FindGatedParts(options, types));

    /// <inheritdoc/>
    public IReadOnlyCollection<Type> GatedParts => _gatedParts.Value;

    /// <inheritdoc/>
    public bool IsGated(Type part) => PathOf(part).Count > 0;

    /// <inheritdoc/>
    public async ValueTask<AvailabilityDecision> Decide(Type part, IServiceProvider services, CancellationToken cancellationToken)
    {
        var path = PathOf(part);
        if (path.Count == 0)
        {
            return AvailabilityDecision.Available;
        }

        var resolver = services.GetRequiredService<AvailabilityRequestContext>().ResolverFor(options.ScopeFor(part));
        var result = await evaluator.EvaluatePath(path, resolver, cancellationToken);
        return result.Outcome switch
        {
            AvailabilityOutcome.Available => AvailabilityDecision.Available,
            AvailabilityOutcome.Unavailable => new AvailabilityDecision(AvailabilityVerdict.Unavailable, result.Causes),
            _ => new AvailabilityDecision(AvailabilityVerdict.Unknown, result.Causes)
        };
    }

    /// <inheritdoc/>
    public IDisposable OnChanged(Func<ValueTask> handler) => audienceSource.OnChanged(_ => handler());

    static IReadOnlyCollection<Type> FindGatedParts(ArcAvailabilityOptions options, ITypes types)
    {
        var declared = types.All.Where(type =>
            (type.IsClass || type.IsValueType) &&
            (type.IsDefined(typeof(AvailableForAttribute), false) || type.IsDefined(typeof(AvailableWithAttribute), false)));
        var registered = options.Parts.Select(_ => _.Part);
        var inNamespaces = types.All.Where(type => type.Namespace is not null && options.Namespaces.Any(_ => InNamespace(type, _.Namespace)));
        return [.. declared.Concat(registered).Concat(inNamespaces).Distinct()];
    }

    static bool InNamespace(Type type, string @namespace) =>
        type.Namespace is { } typeNamespace && (typeNamespace == @namespace || typeNamespace.StartsWith(@namespace + ".", StringComparison.Ordinal));

    IReadOnlyList<AvailabilityExpression> PathOf(Type part) => _paths.GetOrAdd(part, BuildPath);

    List<AvailabilityExpression> BuildPath(Type part)
    {
        var path = new List<AvailabilityExpression>();
        path.AddRange(options.Namespaces
            .Where(_ => InNamespace(part, _.Namespace))
            .OrderBy(_ => _.Namespace.Length)
            .Select(_ => _.Gate));
        path.AddRange(options.Parts.Where(_ => _.Part == part).Select(_ => _.Gate));
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
}
