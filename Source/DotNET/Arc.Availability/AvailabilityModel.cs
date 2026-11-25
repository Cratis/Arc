// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents what the application declares for availability, together with what a developer has switched.
/// </summary>
/// <param name="Overrides">What has been switched on or off.</param>
/// <param name="Audiences">The audiences the application declares.</param>
/// <param name="Blocks">The reusable availability definitions.</param>
/// <param name="Gates">The gates, each guarding a part of the application.</param>
/// <param name="ContextValues">The context values gates can read.</param>
/// <param name="Personas">The named sets of audiences.</param>
public record AvailabilityModel(
    AvailabilityOverrides Overrides,
    IReadOnlyList<AvailabilityAudienceModel> Audiences,
    IReadOnlyList<AvailabilityBlockModel> Blocks,
    IReadOnlyList<AvailabilityGateModel> Gates,
    IReadOnlyList<AvailabilityContextValueModel> ContextValues,
    IReadOnlyList<AvailabilityPersonaModel> Personas)
{
    /// <summary>
    /// Creates the model from the declared audiences and the manifest of the application.
    /// </summary>
    /// <param name="overrides">What has been switched on or off.</param>
    /// <param name="declared">The declared audiences, if the host registered any.</param>
    /// <param name="manifest">The manifest, if the host registered one.</param>
    /// <returns>The <see cref="AvailabilityModel"/>.</returns>
    public static AvailabilityModel From(AvailabilityOverrides overrides, IDeclaredAudiences? declared, IAvailabilityManifest? manifest)
    {
        var current = manifest?.Current ?? AvailabilityManifest.Empty;
        var audiences = (declared?.Declarations ?? [])
            .Select(_ => new AvailabilityAudienceModel(_.Name.Value, _.Description, _.Source))
            .Concat(current.Audiences.Select(_ => new AvailabilityAudienceModel(_.Name.Value, _.Description, _.Source)))
            .GroupBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => _.OrderByDescending(audience => audience.Description.Length).First())
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ToArray();

        return new(
            overrides,
            audiences,
            [.. current.Blocks.Select(_ => new AvailabilityBlockModel(_.Name, _.Description, _.Source, Alternatives(_.Alternatives)))],
            [.. current.Gates.Select(_ => new AvailabilityGateModel(_.Target, Alternatives(_.Alternatives)))],
            [.. current.ContextValues.Select(_ => new AvailabilityContextValueModel(_.Name, _.Type, _.Origin, _.Required))],
            [.. current.Personas.Select(_ => new AvailabilityPersonaModel(_.Name, [.. _.Audiences.Select(audience => audience.Value)]))]);
    }

    static AvailabilityAlternativeModel[] Alternatives(IReadOnlyList<AvailabilityAlternative> alternatives) =>
        [.. alternatives.Select(_ => new AvailabilityAlternativeModel([.. _.Audiences.Select(audience => audience.Value)], _.Conditions))];
}
