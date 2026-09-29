// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// Elements of a collection are walked under the collection's own path, without an index. A dictionary is walked as
/// its key-value pairs; a null element is skipped.
/// </summary>
public class with_collections : given.a_recording_model_graph_validator
{
    void Because() => Walk(new Root(
        [new Item("a"), null!, new Item("b")],
        [new Item("c")],
        new Dictionary<string, Item> { ["k"] = new Item("d") }));

    [Fact] void should_walk_elements_under_the_collection_path() =>
        Traversal.ShouldEqual(
            "Root@ | List`1@list | Item@list | String@list.name | Item@list | String@list.name | " +
            "Item[]@array | Item@array | String@array.name | " +
            "Dictionary`2@byKey | KeyValuePair`2@byKey | String@byKey.key | Item@byKey.value | String@byKey.value.name");

    record Item(string Name);

    record Root(List<Item> List, Item[] Array, Dictionary<string, Item> ByKey);
}
