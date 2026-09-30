// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_walking_members;

/// <summary>
/// A member is walked by the runtime type of its value, not its declared type: an object-typed member reaches the
/// members of what it holds, and a member declared as a base type reaches the members of the subtype it holds.
/// </summary>
public class with_runtime_typed_members : given.a_recording_model_graph_validator
{
    void Because() => Walk(new Root(new Item("held"), new Special("base", "special")));

    [Fact] void should_walk_values_by_their_runtime_type() =>
        Traversal.ShouldEqual("Root@ | Item@payload | String@payload.name | Special@general | String@general.extra | String@general.name");

    record Item(string Name);

    record General(string Name);

    record Special(string Name, string Extra) : General(Name);

    record Root(object Payload, General General);
}
