// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_handle_is_inherited : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("internal")]
    public async Task should_accept_a_base_handle_used_by_a_command(string visibility) => await VerifyValid($@"
        public record Parent {{ {visibility} void Handle([GeneratedValue] CartId value) {{ }} }}
        [Command] public record Allocate : Parent;");

    [Fact] async Task should_accept_a_constructed_generic_base_handle() => await VerifyValid(@"
        public record Parent<T> where T : IGeneratable<T>
        {
            public void Handle([GeneratedValue] T value) { }
        }
        [Command] public record Allocate : Parent<CartId>;");

    [Fact] async Task should_reject_a_private_base_handle_that_the_runtime_cannot_find() => await VerifyDiagnostic(
        @"
        public record Parent { private void Handle([{|#0:GeneratedValue|}] CartId value) { } }
        [Command] public record Allocate : Parent;",
        "ARC0023",
        "value");

    [Fact] async Task should_reject_a_hidden_base_handle_not_used_by_any_command() => await VerifyDiagnostic(
        @"
        public record Parent { public void Handle([{|#0:GeneratedValue|}] CartId value) { } }
        [Command] public record Allocate : Parent { public new void Handle(CartId value) { } }",
        "ARC0023",
        "value");
}
