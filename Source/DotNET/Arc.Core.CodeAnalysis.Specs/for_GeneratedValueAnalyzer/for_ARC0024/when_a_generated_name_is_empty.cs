// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0024;

public class when_a_generated_name_is_empty : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task should_match_the_runtime_non_empty_name_requirement(string name) => await VerifyDiagnostic(
        $@"
        [Command]
        public record Allocate
        {{
            public void Handle([{{|#0:GeneratedValue(""{name}"")|}}] CartId value) {{ }}
        }}",
        "ARC0024",
        "value",
        "Allocate");
}
