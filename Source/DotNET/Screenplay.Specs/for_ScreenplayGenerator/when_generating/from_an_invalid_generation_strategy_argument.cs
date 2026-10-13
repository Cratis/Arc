// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_an_invalid_generation_strategy_argument : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.NanoId(0)")]
    [InlineData("GenerateValue.NanoId(257)")]
    [InlineData("GenerateValue.NanoId(64, \"aa\")")]
    [InlineData("GenerateValue.NanoId(64, \"a\")")]
    [InlineData("GenerateValue.NanoId(64, \"a\\n\")")]
    [InlineData("GenerateValue.Bytes(0)")]
    [InlineData("GenerateValue.Bytes(513)")]
    [InlineData("GenerateValue.TypeId(\"_user\")")]
    [InlineData("GenerateValue.TypeId(\"user_\")")]
    [InlineData("GenerateValue.TypeId(\"User\")")]
    [InlineData("GenerateValue.NanoId(16 + 16)")]
    [InlineData("Guid.NewGuid().ToString()")]
#if NET9_0_OR_GREATER
    [InlineData("System.Security.Cryptography.RandomNumberGenerator.GetHexString(63, lowercase: true)")]
    [InlineData("System.Security.Cryptography.RandomNumberGenerator.GetHexString(1026)")]
#endif
    public void should_withhold_the_entire_generation_claim(string call)
    {
        Generate((Analyzed.SlicePath, Source(call).Replace("AuthorId Receipt", "AuthorId? Receipt", StringComparison.Ordinal)));
        Result.Source.ShouldNotContain("generate ");
        Result.Source.ShouldNotContain(" generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertDocument();
    }
}
