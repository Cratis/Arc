// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_default_case_generation : a_generation_strategy
{
    [Theory]
    [InlineData("GenerateValue.Uuid().ToString(\"D\").ToLowerInvariant()", "generate uuid")]
    [InlineData("GenerateValue.UuidV7().ToString().ToLowerInvariant()", "generate uuidv7")]
    [InlineData("GenerateValue.Ulid().ToUpperInvariant()", "generate ulid")]
    [InlineData("GenerateValue.Bytes(16, ValueEncoding.Hex).ToLowerInvariant()", "generate bytes 16 hex")]
    [InlineData("GenerateValue.Bytes(16, ValueEncoding.Base32).ToUpperInvariant()", "generate bytes 16 base32")]
    public void should_not_add_a_redundant_case_option(string call, string line)
    {
        Generate((Analyzed.SlicePath, Source(call)));
        AssertStrategy(line);
        Result.Source.ShouldNotContain(" case ");
    }
}
