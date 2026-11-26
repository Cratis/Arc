// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies dependency-first acquisition for validator constructors.
/// </summary>
public class when_a_validator_constructor_has_a_reversed_chain : given.a_chained_lookup
{
    ChainValidator _validator;
    async Task Because() => _validator = await BindValidator();
    [Fact] void should_acquire_the_source_first() => _reads.ShouldEqual(["first", "second"]);
    [Fact] void should_keep_the_target_constructor_slot() => _validator.Target.ShouldEqual(_second);
    [Fact] void should_keep_the_source_constructor_slot() => _validator.First.ShouldEqual(_first);
}
