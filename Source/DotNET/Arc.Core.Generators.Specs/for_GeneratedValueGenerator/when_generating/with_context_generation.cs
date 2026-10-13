// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Generators.Specs.for_GeneratedValueGenerator.when_generating;

public class with_context_generation : given.a_generated_value_compilation
{
    object _value;
    readonly GenerationContext _context = new(new DateTimeOffset(2024, 6, 15, 12, 34, 56, TimeSpan.Zero), "tenant-one");

    void Establish() => _input = """
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Concepts;

        public record CartId(string Value) : ConceptAs<string>(Value), IGeneratable<CartId>, IGeneratableInContext<CartId>
        {
            static CartId IGeneratable<CartId>.New() => new("plain");
            static CartId IGeneratableInContext<CartId>.New(GenerationContext context) => new($"{context.Now:O}|{context.Tenant}");
        }

        [Command]
        public record AllocateCartId
        {
            public CartId Handle([GeneratedValue] CartId cartId) => cartId;
        }
        """;

    void Because()
    {
        Generate();
        _value = Create("AllocateCartId", _context);
    }

    [Fact] void should_compile_without_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_emit_a_context_factory() => _source.ShouldContain("static context => global::Cratis.Arc.Commands.GeneratedValueFactories.New<global::CartId>(context)");
    [Fact] void should_pass_the_captured_context() => _value.GetType().GetProperty("Value")!.GetValue(_value).ShouldEqual($"{_context.Now:O}|{_context.Tenant}");
    [Fact] void should_register_before_the_no_dynamic_code_startup_check() => _value.ShouldNotBeNull();
    [Fact] void should_not_emit_the_plain_factory() => _source.ShouldNotContain("New<global::CartId>()");
}
