// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.PersonalAccessTokens.for_PatFormat;

public class when_generating_a_token : Specification
{
    PatFormat _token;
    string _revealed;

    void Because()
    {
        _token = PatFormat.Create(new PatResourceKey("direct"));
        _revealed = _token.Reveal();
    }

    [Fact] void should_use_32_bytes_of_entropy() => _token.Secret.Length.ShouldEqual(32);
    [Fact] void should_not_use_the_fixed_vector_payload() => _token.Secret.ToArray().All(value => value == 0).ShouldBeFalse();
    [Fact] void should_generate_a_different_payload_each_time() => PatFormat.Create(new PatResourceKey("direct")).Reveal().ShouldNotEqual(_revealed);
    [Fact] void should_emit_exactly_64_lowercase_hex_characters() => _revealed.Split('_')[3].All(character => character is (>= '0' and <= '9') or (>= 'a' and <= 'f')).ShouldBeTrue();
    [Fact] void should_emit_the_canonical_length() => _revealed.Length.ShouldEqual(87);
    [Fact] void should_round_trip() => PatFormat.TryParse(_revealed, out _).ShouldBeTrue();
    [Fact] void should_not_disclose_the_token_through_string_formatting() => $"{_token}".ShouldEqual(nameof(PatFormat));
    [Fact] void should_not_disclose_a_parsed_token_through_string_formatting()
    {
        PatFormat.TryParse(_revealed, out var parsed).ShouldBeTrue();
        $"{parsed}".ShouldEqual(nameof(PatFormat));
    }

    [Fact]
    void should_checksum_the_exact_ascii_body()
    {
        var body = _revealed[..^9];
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(body))).ToLowerInvariant()[..8];
        _revealed[^8..].ShouldEqual(expected);
    }
}
