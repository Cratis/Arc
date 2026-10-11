// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_GeneratedValueGenerator.when_generating;

public class with_a_non_generatable_parameter : given.a_generated_value_compilation
{
    void Establish() => _input = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        [Command] public record CreatesId { public void Handle([GeneratedValue] Guid id) { } }
        """;

    void Because() => Generate();

    [Fact] void should_not_emit_an_invalid_factory() => _source.ShouldEqual(string.Empty);
    [Fact] void should_not_break_the_compilation() => _diagnostics.ShouldBeEmpty();
}
