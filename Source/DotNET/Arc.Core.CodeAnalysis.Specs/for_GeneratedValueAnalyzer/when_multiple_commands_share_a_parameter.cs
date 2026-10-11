// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer;

public class when_multiple_commands_share_a_parameter : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("ARC0022", "[{|#0:GeneratedValue|}] Guid value", "")]
    [InlineData("ARC0024", "[GeneratedValue] CartId first, [{|#0:GeneratedValue(\"first\")|}] CartId value", "")]
    [InlineData("ARC0025", "[{|#0:GeneratedValue|}] CartId value", "public CartId Provide() => CartId.New();")]
    public async Task should_report_each_rule_once_at_the_declaring_parameter(string id, string parameters, string provide) => await VerifyDiagnostic(
        $@"
        public record CreateBase
        {{
            public void Handle({parameters}) {{ }}
            {provide}
        }}
        [Command] public record First : CreateBase;
        [Command] public record Second : CreateBase;",
        id,
        "value");
}
