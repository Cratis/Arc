// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_random_generation : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.Integer(-9, 9)", "long", "generate random -9..9")]
    [InlineData("(int)GenerateValue.Integer(-9, 9)", "int", "generate random -9..9")]
    [InlineData("GenerateValue.Integer(-9223372036854775808, 9223372036854775807)", "long", "generate random -9223372036854775808..9223372036854775807")]
    public void should_recover_the_signed_range(string call, string primitive, string line)
    {
        Generate((Analyzed.SlicePath, Source(call, primitive)));
        AssertStrategy(line);
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    }

    [Theory]
    [InlineData("(int)GenerateValue.Integer(-2147483649, 9)", "int")]
    [InlineData("(int)GenerateValue.Integer(1, 2147483648)", "int")]
    [InlineData("(int)(int)GenerateValue.Integer(1, 9)", "int")]
    [InlineData("(ulong)GenerateValue.Integer(1, 9)", "ulong")]
    [InlineData("GenerateValue.Integer(9, 1)", "long")]
    public void should_refuse_lossy_or_invalid_ranges(string call, string primitive)
    {
        Generate((Analyzed.SlicePath, Source(call, primitive)));
        Result.Source.ShouldNotContain("generate random");
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }
}
