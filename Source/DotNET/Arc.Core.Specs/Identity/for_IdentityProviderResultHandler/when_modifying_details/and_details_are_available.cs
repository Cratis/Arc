// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_modifying_details;

public class and_details_are_available : given.an_identity_provider_result_handler
{
    TestDetails _originalDetails;
    TestDetails _modifiedDetails;

    void Establish()
    {
        _originalDetails = new TestDetails("Engineering", "Developer");
        _modifiedDetails = new TestDetails("Marketing", "Manager");

        _identityProvider.Provide(Arg.Any<IdentityProviderContext>())
            .Returns(Task.FromResult(new IdentityDetails(true, _originalDetails)));

        var user = CreateAuthenticatedUser();
        _httpRequestContext.User.Returns(user);
        _httpRequestContext.IsHttps.Returns(true);
    }

    async Task Because() => await _handler.ModifyDetails<TestDetails>(details => _modifiedDetails);

    [Fact] void should_call_write_with_modified_details() =>
        _httpRequestContext.Received(1).WriteAsync(Arg.Is<string>(json => json.Contains("Marketing") && json.Contains("Manager")));

    [Fact] void should_append_cookie_with_correct_content()
    {
        var calls = _httpRequestContext.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IHttpRequestContext.AppendCookie))
            .ToList();

        calls.Count.ShouldEqual(1);

        var cookieValue = calls[0].GetArguments()[1] as string;
        cookieValue.ShouldNotBeNull();

        var decodedJson = Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue!));

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = JsonSerializer.Deserialize<IdentityProviderResult>(decodedJson, serializerOptions);
        result.ShouldNotBeNull();

        var actualDetails = JsonSerializer.Deserialize<TestDetails>(((JsonElement)result.Details).GetRawText(), serializerOptions);
        actualDetails.ShouldEqual(_modifiedDetails);
    }

    public record TestDetails(string Department, string Role);
}
