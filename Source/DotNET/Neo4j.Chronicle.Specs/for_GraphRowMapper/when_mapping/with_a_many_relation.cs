// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphRowMapper.when_mapping;

public class with_a_many_relation : Specification
{
    GraphRow _result;
    IReadOnlyList<GraphRow> _groups;

    void Because()
    {
        var row = new Dictionary<string, object>
        {
            ["id"] = "p1",
            ["name"] = "Ada",
            ["groups"] = new List<object>
            {
                new Dictionary<string, object> { ["id"] = "g1", ["name"] = "Engineering", ["since"] = new ZonedDateTime(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)) },
                new Dictionary<string, object?> { ["id"] = "g2", ["name"] = null, ["since"] = null }
            }
        };
        _result = GraphRowMapper.Map(row, OrganizationViews.Node("PersonWithGroups"));
        _groups = (IReadOnlyList<GraphRow>)_result["groups"]!;
    }

    [Fact] void should_map_a_property_of_the_root() => _result["name"].ShouldEqual("Ada");
    [Fact] void should_map_every_related_item_in_order() => _groups.Select(_ => _["id"]).ShouldEqual(new object?[] { "g1", "g2" });
    [Fact] void should_convert_an_edge_property_from_the_driver() => _groups[0]["since"].ShouldEqual("2026-01-01T00:00:00Z");
    [Fact] void should_keep_an_absent_property_of_a_bare_node_as_null() => _groups[1]["name"].ShouldBeNull();
}
