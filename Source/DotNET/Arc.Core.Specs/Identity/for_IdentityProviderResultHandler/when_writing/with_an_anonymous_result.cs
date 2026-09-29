// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Text.Json;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_writing;

public class with_an_anonymous_result : given.an_identity_provider_result_handler
{
    string _expectedJson;
    string _writtenJson;

    void Establish()
    {
        _expectedJson = JsonSerializer.Serialize(IdentityProviderResult.Anonymous, given.representative_identity.OptionsAsBefore(_options));
        _httpRequestContext
            .When(_ => _.Write(Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(call => _writtenJson = call.Arg<string>());
    }

    async Task Because() => await _handler.SetCookieForHttpResponse(IdentityProviderResult.Anonymous);

    [Fact] void should_write_the_same_json_as_before() => _writtenJson.ShouldEqual(_expectedJson);
    [Fact] void should_leave_out_the_missing_details() => _writtenJson.ShouldNotContain("details");
}
