// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_OnBehalfOfRequestPath;

public class when_normalizing : Specification
{
    string? _unreserved;
    string? _lowercaseHex;
    string? _nonAscii;
    string? _encodedSlash;
    string? _lowercaseEncodedSlash;
    string? _invalidUtf8;

    void Because()
    {
        _unreserved = OnBehalfOfRequestPath.Normalize("/api/%74ickets");
        _lowercaseHex = OnBehalfOfRequestPath.Normalize("/caf%c3%a9");
        _nonAscii = OnBehalfOfRequestPath.Normalize("/café");
        _encodedSlash = OnBehalfOfRequestPath.Normalize("/a%2Fb");
        _lowercaseEncodedSlash = OnBehalfOfRequestPath.Normalize("/a%2fb");
        _invalidUtf8 = OnBehalfOfRequestPath.Normalize("/caf%C3");
    }

    [Fact] void should_decode_an_escaped_unreserved_character() => _unreserved.ShouldEqual("/api/tickets");
    [Fact] void should_decode_escaped_utf8_with_lowercase_hex() => _lowercaseHex.ShouldEqual("/café");
    [Fact] void should_leave_a_decoded_path_as_it_is() => _nonAscii.ShouldEqual("/café");
    [Fact] void should_keep_an_encoded_slash_encoded() => _encodedSlash.ShouldEqual("/a%2Fb");
    [Fact] void should_keep_a_lowercase_encoded_slash_encoded() => _lowercaseEncodedSlash.ShouldEqual("/a%2Fb");
    [Fact] void should_refuse_invalid_utf8() => _invalidUtf8.ShouldBeNull();
}
