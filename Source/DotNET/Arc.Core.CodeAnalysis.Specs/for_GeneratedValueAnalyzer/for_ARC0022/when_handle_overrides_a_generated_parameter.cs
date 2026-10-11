// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_handle_overrides_a_generated_parameter : given.a_generated_value_analyzer
{
    [Fact] async Task should_report_an_invalid_type_at_the_overriding_parameter() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public virtual void Handle([GeneratedValue] Guid original) { }
        }
        [Command] public record Allocate : Parent
        {
            public override void Handle(Guid {|#0:value|}) { }
        }",
        "ARC0022",
        "value",
        "Guid");

    [Fact] async Task should_accept_a_generatable_overriding_parameter() => await VerifyValid(@"
        public record Parent
        {
            public virtual void Handle([GeneratedValue] CartId original) { }
        }
        [Command] public record Allocate : Parent
        {
            public override void Handle(CartId value) { }
        }");

    [Theory]
    [InlineData("Guid")]
    [InlineData("CartId")]
    public async Task should_agree_with_runtime_attribute_inheritance(string type) => await VerifyRuntimeParity($@"
        public record Parent
        {{
            public virtual void Handle([GeneratedValue] {type} original) {{ }}
        }}
        [Command] public record Allocate : Parent
        {{
            public override void Handle({type} {{|#0:value|}}) {{ }}
        }}");

    [Theory]
    [InlineData("Guid")]
    [InlineData("CartId")]
    public async Task should_agree_for_a_constructed_generic_base_override(string type) => await VerifyRuntimeParity($@"
        public record Parent<T>
        {{
            public virtual void Handle([GeneratedValue] T original) {{ }}
        }}
        [Command] public record Allocate : Parent<{type}>
        {{
            public override void Handle({type} {{|#0:value|}}) {{ }}
        }}");
}
