// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

[ReadModel]
public record ProjectionReadModel(string Name)
{
    public static IEnumerable<ProjectionReadModel> Search([Required] string text) => [];

    public static IEnumerable<ProjectionReadModel> All() => [];
}
