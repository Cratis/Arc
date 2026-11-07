// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.for_PatFormat;

public class when_parsing_noncanonical_input : Specification
{
    [Theory]
    [InlineData("uppercase")]
    [InlineData("leading-space")]
    [InlineData("trailing-space")]
    [InlineData("newline")]
    [InlineData("extra-segment")]
    [InlineData("short-checksum")]
    [InlineData("long-checksum")]
    [InlineData("short-secret")]
    [InlineData("long-secret")]
    [InlineData("nonhex-secret")]
    [InlineData("uppercase-secret")]
    [InlineData("nonhex-checksum")]
    [InlineData("uppercase-checksum")]
    [InlineData("unsupported-version")]
    [InlineData("unknown-key")]
    [InlineData("unicode-key")]
    [InlineData("missing-segments")]
    [InlineData("empty")]
    [InlineData("null")]
    void should_refuse_without_producing_a_parsed_token(string mutation)
    {
        var token = FormatVectors.Token("direct");
        var body = token[..^9];

        // Mutated bodies have correct checksums: a checksum must not validate a noncanonical body.
        var inputs = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["uppercase"] = FormatVectors.WithChecksum(body.ToUpperInvariant()),
            ["leading-space"] = " " + token,
            ["trailing-space"] = token + " ",
            ["newline"] = token + "\n",
            ["extra-segment"] = token + "_extra",
            ["short-checksum"] = token[..^1],
            ["long-checksum"] = token + "0",
            ["short-secret"] = FormatVectors.WithChecksum(body.Remove(14, 1)),
            ["long-secret"] = FormatVectors.WithChecksum(body.Insert(14, "0")),
            ["nonhex-secret"] = FormatVectors.WithChecksum(body.Remove(14, 1).Insert(14, "g")),
            ["uppercase-secret"] = FormatVectors.WithChecksum(body.Remove(14, 1).Insert(14, "A")),
            ["nonhex-checksum"] = token[..^1] + "g",
            ["uppercase-checksum"] = token[..^8] + token[^8..].ToUpperInvariant(),
            ["unsupported-version"] = FormatVectors.WithChecksum(body.Replace("_v1_", "_v2_", StringComparison.Ordinal)),
            ["unknown-key"] = FormatVectors.WithChecksum(body.Replace("direct_", "unknown_", StringComparison.Ordinal)),
            ["unicode-key"] = FormatVectors.WithChecksum(body.Replace("direct_", "dіrect_", StringComparison.Ordinal)),
            ["missing-segments"] = "direct_pat_",
            ["empty"] = string.Empty,
            ["null"] = null
        };
        PatFormat.TryParse(inputs[mutation], out var parsed).ShouldBeFalse();
        parsed.ShouldBeNull();
    }
}
