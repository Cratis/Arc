// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies original argument slots across a reversed three-hop acquisition.
/// </summary>
public class when_binding_a_reversed_chain : given.a_chained_lookup
{
    object?[] _arguments;

    async Task Because() => _arguments = await Bind();

    [Fact] void should_acquire_sources_before_dependents() => _reads.ShouldEqual(["first", "second", "third"]);
    [Fact] void should_keep_original_parameter_slots() => _arguments.ShouldEqual([_third, _second, _first]);
    [Fact] void should_acquire_all_models_in_the_same_unit() => _reader.Received(3).GetFor<Node>(_unit, Arg.Any<ReadModelKey>(), Arg.Any<CancellationToken>());
}
