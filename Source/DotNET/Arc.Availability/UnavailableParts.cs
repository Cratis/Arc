// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Suspensions;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the default <see cref="IUnavailableParts"/>, deciding every gated part for the current request.
/// </summary>
/// <param name="gate">The <see cref="IAvailabilityGate"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/> of the request.</param>
/// <param name="discovered">The <see cref="DiscoveredParts"/>; every discovered part is decided when given, so a part with no gate can be listed as suspended.</param>
[IgnoreConvention]
public class UnavailableParts(IAvailabilityGate gate, IServiceProvider services, DiscoveredParts? discovered = null) : IUnavailableParts
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<UnavailablePart>> Get(CancellationToken cancellationToken = default)
    {
        var unavailable = new List<UnavailablePart>();
        try
        {
            foreach (var part in discovered is null ? gate.GatedParts : [.. gate.GatedParts.Union(discovered.All)])
            {
                var decision = await gate.Decide(part, services, cancellationToken);
                if (!decision.IsAvailable)
                {
                    unavailable.Add(UnavailablePart.For(part, decision.Reason));
                }
            }
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or AvailabilityNotDecidedException))
        {
            throw new AvailabilityNotDecidedException(ex);
        }

        unavailable.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        return unavailable;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UnavailablePart>> Get(PartQuestion question, CancellationToken cancellationToken = default)
    {
        var part = gate.GatedParts.Concat(discovered?.All ?? []).FirstOrDefault(_ => (_.FullName ?? _.Name) == question.Part);
        if (part is null)
        {
            return [];
        }

        try
        {
            var decision = gate is IAvailabilityExecutionGate execution
                ? await execution.Decide(new AvailabilityInvocation(part, question.Arguments, DateTimeOffset.UtcNow), services, cancellationToken)
                : await gate.Decide(part, services, cancellationToken);
            return decision.IsAvailable ? [] : [UnavailablePart.For(part, decision.Reason)];
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or AvailabilityNotDecidedException))
        {
            throw new AvailabilityNotDecidedException(ex);
        }
    }

    /// <inheritdoc/>
    public IDisposable OnChanged(Func<ValueTask> handler) =>
        gate.OnChanged(() =>
        {
            // This session outlives the request it started in, so what was resolved then is forgotten before deciding again.
            services.GetService<AvailabilityRequestContext>()?.Reset();
            return handler();
        });
}
