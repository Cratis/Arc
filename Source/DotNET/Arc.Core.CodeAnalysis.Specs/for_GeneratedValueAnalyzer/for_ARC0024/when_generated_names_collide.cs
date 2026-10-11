// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0024;

public class when_generated_names_collide : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("[GeneratedValue(\"cart\")] CartId first", "[{|#0:GeneratedValue(\"cart\")|}] TicketId second")]
    [InlineData("[GeneratedValue] CartId first", "[{|#0:GeneratedValue(\"first\")|}] CartId second")]
    [InlineData("[GeneratedValue(null)] CartId first", "[{|#0:GeneratedValue(\"first\")|}] CartId second")]
    public async Task should_report_the_later_parameter_with_the_same_resolved_name(string first, string second) => await VerifyDiagnostic(
        $@"
        [Command]
        public record Allocate
        {{
            public void Handle({first}, {second}) {{ }}
        }}",
        "ARC0024",
        "second",
        "Allocate");
}
