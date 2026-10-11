// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_handle_is_reserved_for_another_assembly : given.a_generated_value_analyzer
{
    [Fact] async Task should_accept_a_public_abstract_base_without_a_local_command() => await VerifyValid(@"
        public abstract record CreateBase
        {
            public void Handle([GeneratedValue] CartId value) { }
        }");

    [Fact] async Task should_accept_an_effectively_public_nested_base() => await VerifyValid(@"
        public class Container
        {
            public abstract record CreateBase { protected void Handle([GeneratedValue] CartId value) { } }
        }");

    [Theory]
    [InlineData("public sealed record CreateBase", "public", "Handle")]
    [InlineData("internal abstract record CreateBase", "public", "Handle")]
    [InlineData("public readonly record struct CreateBase", "public", "Handle")]
    [InlineData("public abstract record CreateBase", "private", "Handle")]
    [InlineData("public abstract record CreateBase", "public static", "Handle")]
    [InlineData("public abstract record CreateBase", "public", "Provide")]
    [InlineData("public abstract record CreateBase", "public", "Other")]
    public async Task should_reject_declarations_that_cannot_supply_an_external_handle(string declaration, string modifier, string method) => await VerifyDiagnostic(
        $@"
        {declaration}
        {{
            {modifier} void {method}([{{|#0:GeneratedValue|}}] CartId value) {{ }}
        }}",
        "ARC0023",
        "value");

    [Fact] async Task should_reject_a_public_base_inside_an_internal_container() => await VerifyDiagnostic(
        @"
        internal class Container
        {
            public abstract record CreateBase { public void Handle([{|#0:GeneratedValue|}] CartId value) { } }
        }",
        "ARC0023",
        "value");
}
