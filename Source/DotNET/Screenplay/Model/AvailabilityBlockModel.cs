// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// An availability block the code declares as a type.
/// </summary>
/// <param name="Name">The name of the block.</param>
/// <param name="Description">What the block is for, from the documentation of the type.</param>
/// <param name="Lines">The lines of the block, in authored order. All of them have to hold.</param>
public record AvailabilityBlockModel(string Name, string? Description, IReadOnlyList<AvailabilityRequirementModel> Lines);
