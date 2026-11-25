// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Introspection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.given;

public class a_development_host : an_availability_host
{
    protected DevelopmentAvailability _development;

    protected override IEnumerable<Type> Types => [.. base.Types, typeof(SwitchableCommand)];

    protected static string PathOf(Type type) => string.Join(ApplicationStructureBuilder.PathSeparator, ApplicationStructureBuilder.SegmentsOf(type));

    protected static string FeatureOf(Type type) => string.Join(ApplicationStructureBuilder.PathSeparator, ApplicationStructureBuilder.SegmentsOf(type).SkipLast(1));

    /// <summary>
    /// Decides a part as its own request: what a request resolved is kept for the rest of that request.
    /// </summary>
    /// <param name="part">The part to decide.</param>
    /// <returns>The <see cref="AvailabilityDecision"/>.</returns>
    protected async Task<AvailabilityDecision> Decide(Type part)
    {
        await using var scope = _root.CreateAsyncScope();
        return await _gate.Decide(part, scope.ServiceProvider, CancellationToken.None);
    }

    void Establish() => _development = _root.GetRequiredService<DevelopmentAvailability>();

    [Command]
    public record SwitchableCommand;
}
