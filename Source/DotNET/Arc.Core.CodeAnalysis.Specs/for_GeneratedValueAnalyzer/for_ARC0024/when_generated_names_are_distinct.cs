// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0024;

public class when_generated_names_are_distinct : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("[GeneratedValue] CartId first", "[GeneratedValue] CartId second")]
    [InlineData("[GeneratedValue(\"cart\")] CartId first", "[GeneratedValue(\"Cart\")] CartId second")]
    [InlineData("CartId first", "[GeneratedValue(\"first\")] CartId second")]
    public async Task should_compare_only_generated_names_using_ordinal_comparison(string first, string second) => await VerifyValid($@"
        [Command]
        public record Allocate
        {{
            public void Handle({first}, {second}) {{ }}
        }}");

    [Fact] async Task should_not_compare_names_between_commands() => await VerifyValid(@"
        [Command] public record First { public void Handle([GeneratedValue(""cart"")] CartId value) { } }
        [Command] public record Second { public void Handle([GeneratedValue(""cart"")] CartId value) { } }
        ");
}
