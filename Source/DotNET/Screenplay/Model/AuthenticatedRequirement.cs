// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a policy requiring nothing of the caller beyond being authenticated.
/// </summary>
public record AuthenticatedRequirement : PolicyRequirementModel
{
    /// <summary>
    /// The single instance, since the requirement carries no state.
    /// </summary>
    public static readonly AuthenticatedRequirement Instance = new();
}
