// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_provide_does_not_supply_the_generated_type : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public static CartId Provide() => CartId.New();")]
    [InlineData("public TicketId Provide() => TicketId.New();")]
    [InlineData("public (int Count, (CartId Id, int Extra) Nested) Provide() => (1, (CartId.New(), 2));")]
    [InlineData("public (int Count, Task<CartId> Id) Provide() => (1, Task.FromResult(CartId.New()));")]
    [InlineData("public Cratis.Arc.Validation.ValidationResult Provide() => Cratis.Arc.Validation.ValidationResult.Information(\"ok\");")]
    public async Task should_ignore_values_that_runtime_would_not_match(string provide) => await VerifyValid($@"
        [Command]
        public record Allocate
        {{
            {provide}
            public void Handle([GeneratedValue] CartId value) {{ }}
        }}");

    [Fact] async Task should_ignore_an_unmarked_parameter() => await VerifyValid(@"
        [Command]
        public record Allocate
        {
            public CartId Provide() => CartId.New();
            public void Handle(CartId value) { }
        }");
}
