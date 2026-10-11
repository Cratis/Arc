// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_provide_supplies_the_generated_type : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public CartId Provide() => CartId.New();")]
    [InlineData("private CartId Provide() => CartId.New();")]
    [InlineData("internal CartId Provide() => CartId.New();")]
    [InlineData("public Task<CartId> Provide() => Task.FromResult(CartId.New());")]
    [InlineData("public ValueTask<CartId> Provide() => new(CartId.New());")]
    [InlineData("public (int Count, CartId Id) Provide() => (1, CartId.New());")]
    [InlineData("public Task<(int Count, CartId Id)> Provide() => Task.FromResult((1, CartId.New()));")]
    [InlineData("public Tuple<int, CartId> Provide() => Tuple.Create(1, CartId.New());")]
    public async Task should_warn_that_provide_cannot_override_generation(string provide) => await VerifyDiagnostic(
        $@"
        [Command]
        public record Allocate
        {{
            {provide}
            public void Handle([{{|#0:GeneratedValue|}}] CartId value) {{ }}
        }}",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");

    [Fact] async Task should_warn_even_if_an_unmarked_parameter_can_consume_the_value() => await VerifyDiagnostic(
        @"
        [Command]
        public record Allocate
        {
            public CartId Provide() => CartId.New();
            public void Handle([{|#0:GeneratedValue|}] CartId value, CartId existing) { }
        }",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");
}
