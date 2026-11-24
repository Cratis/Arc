// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A requirement satisfied by any caller, whether anonymous or authenticated.
/// </summary>
public sealed record AnonymousRequirement : PolicyRequirementModel
{
    /// <summary>
    /// The single instance, because an unconditional requirement carries no other information.
    /// </summary>
    public static readonly AnonymousRequirement Instance = new();
}
