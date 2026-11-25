// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_relationship_key_is_blank : given.a_chained_lookup
{
    object?[] _arguments;
    void Establish() => ReturnRead("first", _first with { Next = new(" ") });
    async Task Because() => _arguments = await Bind("Optional");
    [Fact] void should_bind_the_target_as_absent() => _arguments[0].ShouldBeNull();
    [Fact] void should_not_acquire_a_target() => _reads.ShouldEqual(["first"]);
}
