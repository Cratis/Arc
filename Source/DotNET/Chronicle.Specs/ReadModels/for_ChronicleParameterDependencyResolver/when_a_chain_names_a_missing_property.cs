// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_chain_names_a_missing_property : given.a_chained_lookup
{
    Exception? _failure;
    void Because() => _failure = Validate("Missing");
    [Fact] void should_refuse_the_declaration() => _failure.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_name_the_property() => _failure!.Message.ShouldContain("Missing");
    [Fact] void should_not_acquire_models() => _reads.ShouldBeEmpty();
}
