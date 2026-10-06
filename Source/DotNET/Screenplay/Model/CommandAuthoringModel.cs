// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents command intent that is valid authoring syntax but not executable.
/// </summary>
public record CommandAuthoringModel
{
    /// <summary>Gets the generated UUID concepts.</summary>
    public IReadOnlyList<PropertyModel> Generated { get; init; } = [];

    /// <summary>Gets the generated event source identity, if it was returned.</summary>
    public string? Identifier { get; init; }

    /// <summary>Gets the scalar response source.</summary>
    public string? Response { get; init; }

    /// <summary>Gets the fields of an unnamed record response.</summary>
    public IReadOnlyList<PropertyMappingModel> ResponseFields { get; init; } = [];

    /// <summary>Gets the operations in returned order.</summary>
    public IReadOnlyList<OperationModel> Operations { get; init; } = [];

    /// <summary>Gets the command's source-owned stream route.</summary>
    public CommandRouteModel? Route { get; init; }

    /// <summary>Gets the command's reliably keyed reads.</summary>
    public IReadOnlyList<CommandReadModel> Reads { get; init; } = [];

    /// <summary>Gets the requirements recovered from provisioning guards.</summary>
    public IReadOnlyList<CommandRequirementModel> Requirements { get; init; } = [];
}
