// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents an <see cref="IAudienceResolver"/> that lets <see cref="DevelopmentAvailability"/> switch audiences on or off
/// for every caller.
/// </summary>
/// <param name="inner">The <see cref="IAudienceResolver"/> that resolves what the caller really belongs to.</param>
/// <param name="development">The <see cref="DevelopmentAvailability"/> holding the overrides.</param>
public class DevelopmentAudienceResolver(IAudienceResolver inner, DevelopmentAvailability development) : IAudienceResolver
{
    /// <inheritdoc/>
    public async ValueTask<EffectiveAudiences> Resolve(AudienceSubject subject, CancellationToken cancellationToken = default)
    {
        var resolved = await inner.Resolve(subject, cancellationToken);
        var overrides = development.Current;
        if (overrides.EnabledAudiences.Count == 0 && overrides.DisabledAudiences.Count == 0)
        {
            return resolved;
        }

        var matching = new HashSet<AudienceName>(resolved.Matching);
        matching.UnionWith(overrides.EnabledAudiences.Select(_ => new AudienceName(_)));
        matching.ExceptWith(overrides.DisabledAudiences.Select(_ => new AudienceName(_)));
        return resolved with { Matching = matching };
    }
}
