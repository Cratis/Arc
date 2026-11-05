// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;
using Cratis.Arc.Neo4j.Testing;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Testing.for_GraphSessionSubstitute;

public class when_querying : Specification
{
    ServiceProvider _provider;
    GraphSessionSubstitute _substitute;
    IReadOnlyList<NamedPerson> _rows;
    IReadOnlyList<NamedPerson> _unmatched;
    NamedPerson? _single;
    PersonId _id;
    IGraphSession _resolved;
    IGraphSession _opened;

    void Establish()
    {
        _id = new PersonId(Guid.NewGuid());
        var services = new ServiceCollection();
        _substitute = services.UseGraphSessionSubstitute();
        _substitute.Namespace = "acme";
        _substitute.Returns("MATCH (p:Person)", new NamedPerson(_id, new PersonName("Ada")));
        _provider = services.BuildServiceProvider();
    }

    async Task Because()
    {
        _resolved = _provider.GetRequiredService<IGraphSession>();
        _opened = _provider.GetRequiredService<IGraphSessionFactory>().Open(AccessMode.Write);
        _rows = await _resolved.Query<NamedPerson>("match (p:Person) where p.personId = $personId return p", new { PersonId = _id });
        _unmatched = await _resolved.Query<NamedPerson>("MATCH (g:Group) RETURN g");
        _single = await _resolved.QuerySingle<NamedPerson>("MATCH (p:Person) RETURN p LIMIT 1");
    }

    [Fact] void should_give_the_substitute_for_the_session() => _resolved.ShouldEqual(_substitute);
    [Fact] void should_give_the_substitute_from_the_factory() => _opened.ShouldEqual(_substitute);
    [Fact] void should_answer_with_the_rows_set_up() => _rows.Count.ShouldEqual(1);
    [Fact] void should_answer_a_single_row() => _single!.PersonId.ShouldEqual(_id);
    [Fact] void should_answer_nothing_for_a_query_not_set_up() => _unmatched.ShouldBeEmpty();
    [Fact] void should_record_every_query() => _substitute.Executions.Count.ShouldEqual(3);
    [Fact] void should_record_the_parameters_as_the_driver_gets_them() => _substitute.Executions[0].Parameters["PersonId"].ShouldEqual(_id.Value.ToString());
    [Fact] void should_add_the_namespace_to_the_parameters() => _substitute.Executions[0].Parameters[GraphParameters.Namespace].ShouldEqual("acme");
}
