// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;

namespace Cratis.Arc.Neo4j.for_GraphParameterConverter;

public class when_converting : Specification
{
    Guid _id;
    IDictionary<string, object?> _result;

    void Establish() => _id = Guid.NewGuid();

    void Because() => _result = GraphParameterConverter.Convert(new
    {
        PersonId = new PersonId(_id),
        Name = new PersonName("Ada"),
        Age = 36,
        Kind = MembershipKind.Owner,
        Aliases = new[] { new PersonName("A"), new PersonName("Lovelace") },
        Nested = new Dictionary<string, object?> { ["id"] = new PersonId(_id) },
        Missing = (string?)null
    });

    [Fact] void should_bind_a_concept_as_its_guid_text() => _result["PersonId"].ShouldEqual(_id.ToString());
    [Fact] void should_bind_a_string_concept_as_its_string() => _result["Name"].ShouldEqual("Ada");
    [Fact] void should_pass_a_primitive_untouched() => _result["Age"].ShouldEqual(36);
    [Fact] void should_bind_an_enum_as_its_name() => _result["Kind"].ShouldEqual("Owner");
    [Fact] void should_unwrap_concepts_in_a_list() => ((List<object?>)_result["Aliases"]!).ShouldContainOnly("A", "Lovelace");
    [Fact] void should_unwrap_concepts_in_a_map() => ((Dictionary<string, object?>)_result["Nested"]!)["id"].ShouldEqual(_id.ToString());
    [Fact] void should_keep_a_null() => _result["Missing"].ShouldBeNull();
}
