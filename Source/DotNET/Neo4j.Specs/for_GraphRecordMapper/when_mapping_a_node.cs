// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_GraphRecordMapper;

public class when_mapping_a_node : Specification
{
    Guid _id;
    NamedPerson _result;

    void Establish() => _id = Guid.NewGuid();

    void Because()
    {
        var node = Substitute.For<INode>();
        node.Properties.Returns(new Dictionary<string, object> { ["personId"] = _id.ToString(), ["name"] = "Ada" });
        var record = Substitute.For<IRecord>();
        record.Keys.Returns(["p"]);
        record[0].Returns(node);
        record["p"].Returns(node);
        _result = GraphRecordMapper.Map<NamedPerson>(record);
    }

    [Fact] void should_map_the_properties_of_the_node() => _result.PersonId.ShouldEqual(new PersonId(_id));
    [Fact] void should_map_a_concept_property_of_the_node() => _result.Name.ShouldEqual(new PersonName("Ada"));
}
