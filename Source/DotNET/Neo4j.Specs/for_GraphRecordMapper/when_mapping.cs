// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_GraphRecordMapper;

public class when_mapping : Specification
{
    Guid _id;
    IRecord _record;
    Membership _result;

    void Establish()
    {
        _id = Guid.NewGuid();
        _record = Substitute.For<IRecord>();
        _record.Keys.Returns(["personId", "name", "age", "kind", "aliases"]);
        _record["personId"].Returns(_id.ToString());
        _record["name"].Returns("Ada");
        _record["age"].Returns(36L);
        _record["kind"].Returns("Owner");
        _record["aliases"].Returns(new List<object> { "A", "Lovelace" });
    }

    void Because() => _result = GraphRecordMapper.Map<Membership>(_record);

    [Fact] void should_build_a_guid_concept_from_its_text() => _result.PersonId.ShouldEqual(new PersonId(_id));
    [Fact] void should_build_a_string_concept() => _result.Name.ShouldEqual(new PersonName("Ada"));
    [Fact] void should_narrow_a_graph_integer() => _result.Age.ShouldEqual(36);
    [Fact] void should_read_an_enum_from_its_name() => _result.Kind.ShouldEqual(MembershipKind.Owner);
    [Fact] void should_build_a_list_of_concepts() => _result.Aliases.ShouldContainOnly(new PersonName("A"), new PersonName("Lovelace"));
}
