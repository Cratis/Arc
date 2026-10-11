// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_provide_returns_a_nullable_struct : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public TicketId? Provide() => TicketId.New();")]
    [InlineData("public Task<TicketId?> Provide() => Task.FromResult<TicketId?>(TicketId.New());")]
    [InlineData("public (int Count, TicketId? Id) Provide() => (1, TicketId.New());")]
    [InlineData("public (int Count, TicketId Id)? Provide() => (1, TicketId.New());")]
    public async Task should_warn_for_the_unwrapped_boxed_value(string provide) => await VerifyDiagnostic(
        $@"
        [Command] public record Allocate
        {{
            {provide}
            public void Handle([{{|#0:GeneratedValue|}}] TicketId value) {{ }}
        }}",
        "ARC0025",
        "value",
        "TicketId",
        "Allocate");
}
