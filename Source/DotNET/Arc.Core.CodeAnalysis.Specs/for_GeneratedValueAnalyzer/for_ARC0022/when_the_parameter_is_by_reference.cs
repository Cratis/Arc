// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_the_parameter_is_by_reference : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("ref")]
    [InlineData("in")]
    public async Task should_match_the_runtime_by_reference_type_rejection(string modifier) => await VerifyDiagnostic(
        $@"
        [Command]
        public record Allocate
        {{
            public void Handle([{{|#0:GeneratedValue|}}] {modifier} CartId value) {{ }}
        }}",
        "ARC0022",
        "value",
        "CartId");
}
