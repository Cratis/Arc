// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// Resolves effective audiences as the union of tenant, user and system grants, split by whether they are declared.
/// </summary>
[IgnoreConvention]
public class AudienceResolver : IAudienceResolver
{
    readonly IAudienceSource _source;
    readonly IDeclaredAudiences _declared;
    readonly AvailabilityOptions _options;
    readonly IUndeclaredAudienceObserver? _observer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudienceResolver"/> class.
    /// </summary>
    /// <param name="source">Where grants come from.</param>
    /// <param name="declared">The audiences the model declares.</param>
    /// <param name="options">The availability options, including the environment switch.</param>
    /// <param name="observer">Optional observer told about undeclared grants.</param>
    /// <exception cref="EnvironmentSwitchRefusedException">The environment switch is on where it is not allowed.</exception>
    public AudienceResolver(
        IAudienceSource source,
        IDeclaredAudiences declared,
        AvailabilityOptions options,
        IUndeclaredAudienceObserver? observer = null)
    {
        options.Validate();
        _source = source;
        _declared = declared;
        _options = options;
        _observer = observer;
    }

    /// <inheritdoc/>
    public async ValueTask<EffectiveAudiences> Resolve(AudienceSubject subject, CancellationToken cancellationToken = default)
    {
        _options.Validate();

        var grants = await _source.GetGrants(subject, cancellationToken);
        var matching = new HashSet<AudienceName>();
        var undeclared = new List<AudienceGrant>();

        foreach (var grant in grants)
        {
            if (_declared.IsDeclared(grant.Audience))
            {
                matching.Add(grant.Audience);
            }
            else
            {
                undeclared.Add(grant);
            }
        }

        if (_options.GrantAllDeclaredAudiences)
        {
            matching.UnionWith(_declared.All);
        }

        if (undeclared.Count > 0)
        {
            _observer?.Observe(subject, undeclared);
        }

        return new EffectiveAudiences(matching, undeclared);
    }
}
