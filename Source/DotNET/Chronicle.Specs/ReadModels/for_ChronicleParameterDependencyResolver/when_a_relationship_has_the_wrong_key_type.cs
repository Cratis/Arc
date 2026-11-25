// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_relationship_has_the_wrong_key_type : given.a_chained_lookup
{
    Exception? _failure;
    void Because() => _failure = Validate("Mismatch");
    [Fact] void should_refuse_the_declaration() => _failure.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_require_an_exact_key_type() => _failure!.Message.ShouldContain("exactly match");
    [Fact] void should_not_acquire_models() => _reads.ShouldBeEmpty();
}
