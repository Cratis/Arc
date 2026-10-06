// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>Represents a reliably returned command operation.</summary>
/// <param name="Name">The operation name.</param>
/// <param name="System">The external system.</param>
/// <param name="Description">The XML summary, if any.</param>
/// <param name="Inputs">The operation's constructor inputs.</param>
/// <param name="Mappings">The inputs' sources.</param>
/// <param name="SourceFilePath">The portable implementation path, if known.</param>
/// <param name="Compensates">Whether a compensation method exists.</param>
public record OperationModel(string Name, string System, string? Description, IReadOnlyList<PropertyModel> Inputs, IReadOnlyList<PropertyMappingModel> Mappings, string? SourceFilePath, bool Compensates)
{
    /// <summary>Gets the exact interface identity, used to reject ambiguous system names.</summary>
    public string SystemTypeIdentity { get; init; } = System;
}
