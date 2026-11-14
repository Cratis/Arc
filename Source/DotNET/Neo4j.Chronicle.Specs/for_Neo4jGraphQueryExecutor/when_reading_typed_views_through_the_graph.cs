// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Arc.Neo4j.Testing;
using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_Neo4jGraphQueryExecutor;

public class when_reading_typed_views_through_the_graph : Specification
{
    Guid _ada;
    Guid _engineering;
    Guid _bare;
    ServiceProvider _provider;
    GraphSessionSubstitute _session;
    IReadOnlyList<PersonWithGroups> _result;

    void Establish()
    {
        (_ada, _engineering, _bare) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(OrganizationViews.Views);
        services.AddCratisNeo4jGraphViews();
        _session = services.UseGraphSessionSubstitute();
        _session.Returns<object>(
            "MATCH (n0:`Person`)",
            new Dictionary<string, object>
            {
                ["id"] = _ada.ToString(),
                ["name"] = "Ada",
                ["groups"] = new List<object>
                {
                    new Dictionary<string, object> { ["id"] = _engineering.ToString(), ["name"] = "Engineering", ["since"] = new ZonedDateTime(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)) },
                    new Dictionary<string, object?> { ["id"] = _bare.ToString(), ["name"] = null, ["since"] = new ZonedDateTime(new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)) }
                }
            });
        _provider = services.BuildServiceProvider();
    }

    async Task Because() => _result = await _provider.CreateAsyncScope().ServiceProvider.GetRequiredService<IGraph>()
        .Nodes<PersonWithGroups>()
        .WhereRelated<GroupMembership>(_ => _.Groups, groups => groups.Where(_ => _.Name, new GroupName("Engineering")))
        .ToList();

    [Fact] void should_read_the_view_with_the_translated_cypher() => _session.Executions[0].Cypher.ShouldContain("WHERE EXISTS { MATCH (n0)-[e1:`MEMBER_OF`]->(n1:`Group`) WHERE n1.`name` = $p0 }");
    [Fact] void should_give_the_concept_as_its_primitive() => _session.Executions[0].Parameters["p0"].ShouldEqual("Engineering");
    [Fact] void should_build_the_root_with_its_concepts() => _result[0].Id.ShouldEqual(new PersonId(_ada));
    [Fact] void should_build_every_related_view() => _result[0].Groups.Select(_ => _.Id).ShouldEqual(new[] { new GroupId(_engineering), new GroupId(_bare) });
    [Fact] void should_build_an_edge_property_from_the_driver_timestamp() => _result[0].Groups[0].Since.ShouldEqual(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    [Fact] void should_give_null_for_an_absent_property_of_a_bare_node() => _result[0].Groups[1].Name.ShouldBeNull();
}
