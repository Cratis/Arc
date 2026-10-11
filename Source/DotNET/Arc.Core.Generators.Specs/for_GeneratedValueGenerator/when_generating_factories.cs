// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_GeneratedValueGenerator;

public class when_generating_factories : given.a_generated_value_compilation
{
    object _value;

    void Establish() => _input = """
        using System;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;

        public record CartId(Guid Value) : ConceptAs<Guid>(Value), IGeneratable<CartId>
        {
            public static CartId New() => new(GenerateValue.Uuid());
        }

        [Command]
        public record AllocateCartId
        {
            public CartId Handle([GeneratedValue("cart")] CartId cartId) => cartId;
        }

        [Command] public record AnotherCart { public void Handle([GeneratedValue] CartId cartId) { } }
        """;

    void Because()
    {
        Generate();
        _value = Create("AllocateCartId");
    }

    [Fact] void should_compile_without_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_in_a_module_initializer() => _source.ShouldContain("ModuleInitializer");
    [Fact] void should_use_the_constrained_helper() => _source.ShouldContain("GeneratedValueFactories.New<global::CartId>()");
    [Fact] void should_register_each_type_once() => (_source.Split("typeof(global::CartId)").Length - 1).ShouldEqual(1);
    [Fact] void should_create_the_declared_type_without_a_reflection_factory() => _value.GetType().ShouldEqual(_assembly.GetType("CartId"));
    [Fact] void should_register_before_the_startup_check() => _value.ShouldNotBeNull();
}
