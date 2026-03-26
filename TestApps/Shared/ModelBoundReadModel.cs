// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace TestApps;

[ReadModel]
public record ModelBoundReadModel(int Data)
{
    public static IQueryable<ModelBoundReadModel> GetAll() => Enumerable.Range(1, 20).Select(i => new ModelBoundReadModel(i)).AsQueryable();
    public static ModelBoundReadModel GetById(string id) => int.TryParse(id, out var num) ? new(num) : new(0);
}
