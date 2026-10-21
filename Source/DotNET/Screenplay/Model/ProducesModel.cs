// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents an event a command produces.
/// </summary>
/// <param name="EventName">The name of the event produced.</param>
/// <param name="When">The condition the production is guarded by, or <see langword="null"/> when it is unconditional.</param>
/// <param name="Mappings">The mappings from command input onto event properties.</param>
public record ProducesModel(
    string EventName,
    ConditionModel? When,
    IEnumerable<PropertyMappingModel> Mappings)
{
    /// <summary>
    /// Gets an unavailable generated dependency of the production's guard.
    /// </summary>
    public UnavailableGeneratedValueModel? UnavailableCondition { get; init; }

    /// <summary>
    /// Gets an unavailable generated dependency of an explicit destination.
    /// </summary>
    public UnavailableGeneratedValueModel? UnavailableDestination { get; init; }

    /// <summary>
    /// Gets the proven explicit destination of an event production.
    /// </summary>
    public MappingSourceModel? Destination { get; init; }

    /// <summary>
    /// Gets the complete effective route overriding command defaults for this production.
    /// </summary>
    public CommandRouteModel? Route { get; init; }

    /// <summary>
    /// Gets generated value paths used by explicit destination or route expressions, including unadmitted expressions.
    /// </summary>
    public IReadOnlyList<string> DestinationDependencies { get; init; } = [];

    /// <summary>
    /// Gets the source event type identity, when the construction was analyzed.
    /// </summary>
    public string? EventTypeIdentity { get; init; }

    /// <summary>
    /// Gets whether the construction was recovered without unsupported control flow or mappings.
    /// </summary>
    public bool CanInline { get; init; }

    /// <summary>
    /// Gets whether the event is returned through command context or applied to an aggregate of that context.
    /// Explicit and unprovable routing is never replaced with the command identifier.
    /// </summary>
    public bool UsesCommandContext { get; init; }
}
