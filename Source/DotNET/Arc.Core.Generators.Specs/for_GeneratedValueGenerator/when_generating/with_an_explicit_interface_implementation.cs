// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_GeneratedValueGenerator.when_generating;

public class with_an_explicit_interface_implementation : given.a_generated_value_compilation
{
    object _value;

    void Establish() => _input = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;
        public sealed record Id(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<Id>
        {
            static Id IGeneratable<Id>.New() => new(Guid.Parse("e6c85cc8-154b-41ce-99a4-b9a9e78e4bce"));
        }
        [Command] public record CreatesId { public void Handle([GeneratedValue] Id id) { } }
        """;

    void Because()
    {
        Generate();
        _value = Create("CreatesId");
    }

    [Fact] void should_compile_the_explicit_implementation() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_call_the_static_interface_member() => _value.GetType().GetProperty("Value")!.GetValue(_value)!.ToString().ShouldEqual("e6c85cc8-154b-41ce-99a4-b9a9e78e4bce");
}
