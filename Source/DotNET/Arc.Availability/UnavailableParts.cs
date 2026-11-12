// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents the default <see cref="IUnavailableParts"/>, deciding every gated part for the current request.
/// </summary>
/// <param name="gate">The <see cref="IAvailabilityGate"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/> of the request.</param>
public class UnavailableParts(IAvailabilityGate gate, IServiceProvider services) : IUnavailableParts
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> Get(CancellationToken cancellationToken = default)
    {
        var unavailable = new List<string>();
        try
        {
            foreach (var part in gate.GatedParts)
            {
                var decision = await gate.Decide(part, services, cancellationToken);
                if (!decision.IsAvailable)
                {
                    unavailable.Add(PartName.Of(part));
                }
            }
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or AvailabilityNotDecidedException))
        {
            throw new AvailabilityNotDecidedException(ex);
        }

        unavailable.Sort(StringComparer.Ordinal);
        return unavailable;
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
