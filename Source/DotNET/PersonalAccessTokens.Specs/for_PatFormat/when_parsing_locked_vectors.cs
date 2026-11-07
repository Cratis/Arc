// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.for_PatFormat;

public class when_parsing_locked_vectors : Specification
{
    [Theory]
    [InlineData("direct")]
    [InlineData("studio")]
    [InlineData("stage-atlas")]
    void should_parse_the_exact_locked_checksum(string resourceKey)
    {
        var token = FormatVectors.Token(resourceKey);
        PatFormat.TryParse(token, out var parsed).ShouldBeTrue();
        parsed!.ResourceKey.Value.ShouldEqual(resourceKey);
        parsed.Secret.Length.ShouldEqual(32);
        parsed.Secret.ToArray().All(value => value == 0).ShouldBeTrue();
        parsed.Reveal().ShouldEqual(token);
    }

    [Fact]
    void should_refuse_the_locked_bad_checksum()
    {
        var token = FormatVectors.Token("direct");
        PatFormat.TryParse(token[..^1] + "9", out var parsed).ShouldBeFalse();
        parsed.ShouldBeNull();
    }
}
