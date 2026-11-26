// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies fail-closed acquisition for a required target with no source.
/// </summary>
public class when_a_required_chain_has_an_absent_source : given.a_chained_lookup
{
    Exception _failure;
    void Establish() => ReturnRead("first", null);
    async Task Because() => _failure = await Catch.Exception(async () => await Bind("Required"));
    [Fact] void should_refuse_missing_required_state() => _failure.ShouldBeOfExactType<ProtectedReadModelDoesNotExist>();
    [Fact] void should_not_acquire_a_target() => _reads.ShouldEqual(["first"]);
}
