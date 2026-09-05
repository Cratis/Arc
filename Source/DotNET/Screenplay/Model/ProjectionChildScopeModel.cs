// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a child collection or nested object of a projection.
/// </summary>
/// <param name="Property">The read model property holding the children or nested object.</param>
/// <param name="IdentifiedBy">The expression identifying each child, empty for a nested object.</param>
/// <param name="AutoMap">How automatic property mapping applies.</param>
/// <param name="Scope">Everything declared within the child or nested scope.</param>
public record ProjectionChildScopeModel(
    string Property,
    string IdentifiedBy,
    ProjectionAutoMapMode AutoMap,
    ProjectionScopeModel Scope);
