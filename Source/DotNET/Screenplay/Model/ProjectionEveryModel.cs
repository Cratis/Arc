// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a block applying to every event a projection observes.
/// </summary>
/// <param name="Properties">The read model property to event expression map.</param>
/// <param name="IncludeChildren">Whether the block also applies to child objects.</param>
/// <param name="AutoMap">How automatic property mapping applies.</param>
public record ProjectionEveryModel(
    IReadOnlyDictionary<string, string> Properties,
    bool IncludeChildren,
    ProjectionAutoMapMode AutoMap);
