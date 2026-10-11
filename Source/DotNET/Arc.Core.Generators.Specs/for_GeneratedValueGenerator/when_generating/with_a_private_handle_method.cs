// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_GeneratedValueGenerator.when_generating;

public class with_a_private_handle_method : given.a_generated_value_compilation
{
    object _value;

    void Establish() => _input = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;
        public record Id(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<Id>
        {
            public static Id New() => new(GenerateValue.Uuid());
        }
        public class Container
        {
            [Command] private record CreatesId { private void Handle([GeneratedValue] Id id) { } }
        }
        """;

    void Because()
    {
        Generate();
        _value = Create("Container+CreatesId");
    }

    [Fact] void should_compile_without_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_accessible_value_type() => _source.ShouldContain("GeneratedValueFactories.New<global::Id>()");
    [Fact] void should_create_the_value_without_a_reflection_factory() => _value.GetType().ShouldEqual(_assembly.GetType("Id"));
}
