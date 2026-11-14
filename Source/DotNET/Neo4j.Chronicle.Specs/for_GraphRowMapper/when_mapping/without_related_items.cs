// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphRowMapper.when_mapping;

public class without_related_items : Specification
{
    GraphRow _many;
    GraphRow _one;
    GraphRow _missing;

    void Because()
    {
        _many = GraphRowMapper.Map(new Dictionary<string, object> { ["id"] = "p3", ["name"] = "Linus", ["groups"] = new List<object>() }, OrganizationViews.Node("PersonWithGroups"));
        _one = GraphRowMapper.Map(new Dictionary<string, object?> { ["id"] = "g1", ["name"] = "Engineering", ["parent"] = null }, OrganizationViews.Node("GroupWithParent"));
        _missing = GraphRowMapper.Map(new Dictionary<string, object> { ["id"] = "p3" }, OrganizationViews.Node("PersonWithGroups"));
    }

    [Fact] void should_give_an_empty_list_for_many() => ((IReadOnlyList<GraphRow>)_many["groups"]!).ShouldBeEmpty();
    [Fact] void should_give_null_for_one() => _one["parent"].ShouldBeNull();
    [Fact] void should_give_an_empty_list_for_a_many_relation_the_row_does_not_hold() => ((IReadOnlyList<GraphRow>)_missing["groups"]!).ShouldBeEmpty();
    [Fact] void should_give_null_for_a_property_the_row_does_not_hold() => _missing.Values["name"].ShouldBeNull();
}
