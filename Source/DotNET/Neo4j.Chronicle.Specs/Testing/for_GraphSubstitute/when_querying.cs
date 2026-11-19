// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Neo4j.Chronicle.Testing.for_GraphSubstitute;

public class when_querying : Specification
{
    ServiceProvider _provider;
    GraphSubstitute _substitute;
    GroupSummary _engineering;
    IGraph _resolved;
    IReadOnlyList<GroupSummary> _groups;
    GroupSummary? _first;
    IReadOnlyList<PersonWithGroups> _people;
    Exception _notAView;

    void Establish()
    {
        _engineering = new(new GroupId(Guid.NewGuid()), new GroupName("Engineering"));
        var services = new ServiceCollection();
        _substitute = services.UseGraphSubstitute(OrganizationViews.Views);
        _substitute.Returns(_engineering);
        _provider = services.BuildServiceProvider();
    }

    async Task Because()
    {
        _resolved = _provider.GetRequiredService<IGraph>();
        _groups = await _resolved.Nodes<GroupSummary>().Where(_ => _.Name, new GroupName("Engineering")).ToList();
        _first = await _resolved.Nodes<GroupSummary>().FirstOrDefault();
        _people = await _resolved.Nodes<PersonWithGroups>().ToList();
        _notAView = Catch.Exception(() => _resolved.Nodes<Person>());
    }

    [Fact] void should_give_the_substitute_for_the_graph() => _resolved.ShouldBeSame(_substitute);
    [Fact] void should_answer_with_the_views_set_up() => _groups.ShouldContainOnly(_engineering);
    [Fact] void should_answer_the_first_view_set_up() => _first.ShouldEqual(_engineering);
    [Fact] void should_answer_nothing_for_a_view_not_set_up() => _people.ShouldBeEmpty();
    [Fact] void should_record_every_query() => _substitute.Executions.Count.ShouldEqual(3);
    [Fact] void should_record_the_clauses_as_production_builds_them() => _substitute.Executions[0].Clauses.ShouldContainOnly(new GraphPropertyClause("name", "name", "Engineering"));
    [Fact] void should_record_the_shape() => _substitute.Executions[1].Shape.ShouldEqual(GraphQueryShape.First);
    [Fact] void should_refuse_a_type_that_is_not_a_view() => _notAView.ShouldBeOfExactType<NotAGraphView>();
}
