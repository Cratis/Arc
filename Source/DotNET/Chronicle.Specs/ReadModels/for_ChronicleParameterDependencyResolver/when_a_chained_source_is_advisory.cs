// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

/// <summary>
/// Verifies refusal of an advisory source for a protected relationship.
/// </summary>
public class when_a_chained_source_is_advisory : given.a_chained_lookup
{
    Exception? _failure;
    void Because() => _failure = Validate("Advisory");
    [Fact] void should_refuse_the_declaration() => _failure.ShouldBeOfExactType<InvalidLookUpBy>();
    [Fact] void should_not_acquire_models() => _reads.ShouldBeEmpty();
}
