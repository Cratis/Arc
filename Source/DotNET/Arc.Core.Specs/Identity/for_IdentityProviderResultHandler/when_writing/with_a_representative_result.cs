// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_writing;

/// <summary>
/// The identity JSON is read by the frontend from the response and the cookie, so it must stay byte for byte what
/// the reflection-based serialization produced.
/// </summary>
public class with_a_representative_result : given.an_identity_provider_result_handler
{
    string _expectedJson;
    string _writtenJson;
    string _cookieValue;

    void Establish()
    {
        _expectedJson = JsonSerializer.Serialize(given.representative_identity.Result, given.representative_identity.OptionsAsBefore(_options));
        _httpRequestContext
            .When(_ => _.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(call => _writtenJson = call.Arg<string>());
        _httpRequestContext
            .When(_ => _.AppendCookie(IdentityProvider.IdentityCookieName, Arg.Any<string>(), Arg.Any<CookieOptions>()))
            .Do(call => _cookieValue = call.ArgAt<string>(1));
    }

    async Task Because() => await _handler.SetCookieForHttpResponse(given.representative_identity.Result);

    [Fact] void should_write_the_same_json_as_before() => _writtenJson.ShouldEqual(_expectedJson);
    [Fact] void should_store_the_same_json_in_the_cookie_as_before() => Encoding.UTF8.GetString(Convert.FromBase64String(_cookieValue)).ShouldEqual(_expectedJson);
    [Fact] void should_not_escape_non_ascii_characters() => _writtenJson.ShouldContain("æøå");
    [Fact] void should_write_concepts_as_their_values() => _writtenJson.ShouldContain("\"id\":\"user-æøå-123\"");
    [Fact] void should_leave_out_null_values() => _writtenJson.ShouldNotContain("nickname");
}
