// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

[ReadModel]
public record StructuredReadModel(string Text)
{
    public static IQueryable<StructuredReadModel> All(bool fail = false) => fail
        ? throw new InvalidOperationException("Internal query failure.")
        : new[] { new StructuredReadModel("first"), new StructuredReadModel("second"), new StructuredReadModel("third") }.AsQueryable();
}
