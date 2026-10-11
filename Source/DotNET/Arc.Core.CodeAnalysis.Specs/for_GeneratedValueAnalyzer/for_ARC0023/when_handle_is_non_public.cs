// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_handle_is_non_public : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("private")]
    [InlineData("protected")]
    public async Task should_accept_every_instance_handle_visibility_the_runtime_discovers(string visibility) => await VerifyValid($@"
        [Command]
        public record Allocate
        {{
            {visibility} void Handle([GeneratedValue] CartId value) {{ }}
        }}");
}
