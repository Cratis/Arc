// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphRowMapper.when_mapping;

public class with_a_one_relation : Specification
{
    GraphRow _result;

    void Because() => _result = GraphRowMapper.Map(
        new Dictionary<string, object> { ["id"] = "g2", ["name"] = "Platform", ["parent"] = new Dictionary<string, object> { ["id"] = "g1", ["name"] = "Engineering" } },
        OrganizationViews.Node("GroupWithParent"));

    [Fact] void should_map_the_related_item_as_a_row() => ((GraphRow)_result["parent"]!)["name"].ShouldEqual("Engineering");
}
