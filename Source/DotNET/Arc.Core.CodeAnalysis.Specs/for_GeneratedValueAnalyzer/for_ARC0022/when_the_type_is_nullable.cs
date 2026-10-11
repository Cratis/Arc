// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_the_type_is_nullable : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("CartId?")]
    [InlineData("TicketId?")]
    [InlineData("Nullable<TicketId>")]
    public async Task should_reject_nullable_reference_and_value_types(string type) => await VerifyDiagnostic(
        $@"
        [Command]
        public record Allocate
        {{
            public void Handle([{{|#0:GeneratedValue|}}] {type} value) {{ }}
        }}",
        "ARC0022",
        "value");
}
