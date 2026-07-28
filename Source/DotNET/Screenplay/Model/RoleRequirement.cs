// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a policy requiring the caller to hold a role.
/// </summary>
/// <param name="Role">The name of the role.</param>
public record RoleRequirement(string Role) : PolicyRequirementModel;
