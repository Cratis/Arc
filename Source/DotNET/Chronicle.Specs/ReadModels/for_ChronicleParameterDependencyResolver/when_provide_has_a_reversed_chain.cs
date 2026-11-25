// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_provide_has_a_reversed_chain : given.a_chained_lookup
{
    object?[] _arguments;
    void Establish() => Select(new Providing(new("first")));
    async Task Because() => _arguments = await Bind();
    [Fact] void should_acquire_the_source_first() => _reads.ShouldEqual(["first", "second", "third"]);
    [Fact] void should_not_replace_explicit_handle_arguments_with_the_provided_model() => _arguments.ShouldEqual([_third, _second, _first]);
}
