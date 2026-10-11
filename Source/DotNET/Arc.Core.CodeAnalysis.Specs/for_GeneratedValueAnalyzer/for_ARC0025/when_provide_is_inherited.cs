// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_provide_is_inherited : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    public async Task should_warn_for_inherited_instance_providers(string visibility) => await VerifyDiagnostic(
        $@"
        public record Parent {{ {visibility} CartId Provide() => CartId.New(); }}
        [Command]
        public record Allocate : Parent
        {{
            public void Handle([{{|#0:GeneratedValue|}}] CartId value) {{ }}
        }}",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");

    [Fact] async Task should_ignore_private_base_providers() => await VerifyValid(@"
        public record Parent { private CartId Provide() => CartId.New(); }
        [Command]
        public record Allocate : Parent
        {
            public void Handle([GeneratedValue] CartId value) { }
        }
        ");
}
