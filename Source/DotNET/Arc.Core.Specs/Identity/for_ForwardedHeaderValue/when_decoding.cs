// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_ForwardedHeaderValue;

public class when_decoding : Specification
{
    string? _verbatim;
    string? _encoded;
    string? _encodedPrefix;
    string? _truncatedEscape;
    string? _nonAsciiInEncoded;

    void Because()
    {
        _verbatim = ForwardedHeaderValue.Decode("plain-user-id");
        _encoded = ForwardedHeaderValue.Decode("UTF-8''S%C3%B8ren%20W%C3%A6rstad");
        _encodedPrefix = ForwardedHeaderValue.Decode("UTF-8''UTF-8%27%27victim");
        _truncatedEscape = ForwardedHeaderValue.Decode("UTF-8''S%C");
        _nonAsciiInEncoded = ForwardedHeaderValue.Decode("UTF-8''Søren");
    }

    [Fact] void should_return_a_value_without_the_prefix_as_it_is() => _verbatim.ShouldEqual("plain-user-id");
    [Fact] void should_decode_an_encoded_value() => _encoded.ShouldEqual("Søren Wærstad");
    [Fact] void should_decode_a_value_whose_original_carried_the_prefix() => _encodedPrefix.ShouldEqual("UTF-8''victim");
    [Fact] void should_refuse_a_truncated_escape() => _truncatedEscape.ShouldBeNull();
    [Fact] void should_refuse_a_non_ascii_character_in_an_encoded_value() => _nonAsciiInEncoded.ShouldBeNull();
}
