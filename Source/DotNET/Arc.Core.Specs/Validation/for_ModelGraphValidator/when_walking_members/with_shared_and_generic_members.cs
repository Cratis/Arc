// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// A reference reached twice is walked once, at the first path it was reached by; a value of a generic type is walked
/// by its constructed type, and a concept is walked like any other member.
/// </summary>
public class with_shared_and_generic_members : given.a_recording_model_graph_validator
{
    void Because()
    {
        var shared = new Item("shared");
        Walk(new Root(shared, shared, new Wrapper<Item>(new Item("wrapped"), 3), new Identifier(Guid.Empty)));
    }

    [Fact] void should_walk_shared_references_once_and_generic_values_by_their_constructed_type() =>
        Traversal.ShouldEqual(
            "Root@ | Item@first | String@first.name | " +
            "Wrapper`1@wrapper | Item@wrapper.value | String@wrapper.value.name | Int32@wrapper.count | " +
            "Identifier@id | Guid@id.value");

    record Identifier(Guid Value) : ConceptAs<Guid>(Value);

    record Item(string Name);

    record Wrapper<T>(T Value, int Count);

    record Root(Item First, Item Second, Wrapper<Item> Wrapper, Identifier Id);
}
