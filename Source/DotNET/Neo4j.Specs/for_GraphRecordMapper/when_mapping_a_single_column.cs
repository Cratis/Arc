// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_GraphRecordMapper;

public class when_mapping_a_single_column : Specification
{
    Guid _id;
    PersonId _concept;
    IReadOnlyList<PersonName> _names;
    string _text;

    void Establish() => _id = Guid.NewGuid();

    void Because()
    {
        _concept = GraphRecordMapper.Map<PersonId>(Record(_id.ToString()));
        _names = GraphRecordMapper.Map<IReadOnlyList<PersonName>>(Record(new List<object> { "Ada", "Grace" }));
        _text = GraphRecordMapper.Map<string>(Record("plain"));
    }

    [Fact] void should_map_the_column_to_a_concept() => _concept.ShouldEqual(new PersonId(_id));
    [Fact] void should_map_a_list_column_to_a_list_of_concepts() => _names.ShouldContainOnly(new PersonName("Ada"), new PersonName("Grace"));
    [Fact] void should_map_the_column_to_a_primitive() => _text.ShouldEqual("plain");

    static IRecord Record(object value)
    {
        var record = Substitute.For<IRecord>();
        record.Keys.Returns(["value"]);
        record[0].Returns(value);
        record["value"].Returns(value);
        return record;
    }
}
