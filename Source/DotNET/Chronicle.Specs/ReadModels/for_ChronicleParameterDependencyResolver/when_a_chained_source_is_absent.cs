// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies absence propagation from a missing relationship source.
/// </summary>
public class when_a_chained_source_is_absent : given.a_chained_lookup
{
    object?[] _arguments;
    void Establish() => ReturnRead("first", null);
    async Task Because() => _arguments = await Bind("Optional");
    [Fact] void should_bind_both_nullable_parameters_as_absent() => _arguments.ShouldEqual([null, null]);
    [Fact] void should_not_acquire_a_target() => _reads.ShouldEqual(["first"]);
    [Fact] void should_keep_the_source_absence_read() => _tokens.Count.ShouldEqual(1);
}
