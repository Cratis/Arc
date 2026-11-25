// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_chain_has_a_cycle : given.a_chained_lookup
{
    Exception? _failure;
    async Task Because() => _failure = await Catch.Exception(async () => await Bind("Cycle"));
    [Fact] void should_refuse_the_declaration() => _failure.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_name_the_member() => _failure!.Message.ShouldContain("Cycle");
    [Fact] void should_report_the_cycle_path() => _failure!.Message.ShouldContain("first -> second -> first");
    [Fact] void should_not_acquire_any_models() => _reads.ShouldBeEmpty();
}
