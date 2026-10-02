// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_writing;

/// <summary>
/// The identity JSON is read by the frontend from the response, so it must stay byte for byte what the
/// reflection-based serialization produced.
/// </summary>
public class with_a_representative_result : given.an_identity_provider_result_handler
{
    string _expectedJson;
    string _writtenJson;

    void Establish()
    {
        _expectedJson = JsonSerializer.Serialize(given.representative_identity.Result, given.representative_identity.OptionsAsBefore(_options));
        _httpRequestContext
            .When(_ => _.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(call => _writtenJson = call.Arg<string>());
    }

    async Task Because() => await _handler.SetCookieForHttpResponse(given.representative_identity.Result);

    [Fact] void should_write_the_same_json_as_before() => _writtenJson.ShouldEqual(_expectedJson);
    [Fact] void should_not_write_an_identity_cookie() => _httpRequestContext.DidNotReceive().AppendCookie(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CookieOptions>());
    [Fact] void should_not_escape_non_ascii_characters() => _writtenJson.ShouldContain("æøå");
    [Fact] void should_write_concepts_as_their_values() => _writtenJson.ShouldContain("\"id\":\"user-æøå-123\"");
    [Fact] void should_leave_out_null_values() => _writtenJson.ShouldNotContain("nickname");
}
