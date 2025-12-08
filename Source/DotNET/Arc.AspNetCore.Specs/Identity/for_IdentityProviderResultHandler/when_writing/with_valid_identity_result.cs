// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_writing;

public class with_valid_identity_result : given.an_identity_provider_result_handler
{
    IdentityProviderResult _identityResult;
    string _expectedJson;
    string _responseContent;
    Microsoft.Net.Http.Headers.SetCookieHeaderValue _identityCookie;

    void Establish()
    {
        _identityResult = new IdentityProviderResult(
            new IdentityId("user123"),
            new IdentityName("Test User"),
            true,
            true,
            new { Department = "Engineering", Role = "Developer" });

        _expectedJson = JsonSerializer.Serialize(_identityResult, _serializerOptions);
        _httpContext.Request.Scheme = "https";
    }

    async Task Because()
    {
        await _handler.Write(_identityResult);

        _httpContext.Response.Body.Position = 0;
        var reader = new StreamReader(_httpContext.Response.Body);
        _responseContent = await reader.ReadToEndAsync();

        var cookies = _httpContext.Response.GetTypedHeaders().SetCookie;
        _identityCookie = cookies.FirstOrDefault(c => c.Name == IdentityProviderResultHandler.IdentityCookieName)!;
    }

    [Fact] void should_set_content_type_to_application_json() => _httpContext.Response.ContentType.ShouldEqual("application/json; charset=utf-8");

    [Fact] void should_write_json_to_response_body() => _responseContent.ShouldEqual(_expectedJson);

    [Fact] void should_set_identity_cookie() => _identityCookie.ShouldNotBeNull();

    [Fact] void should_set_cookie_with_base64_encoded_json()
    {
        var expectedBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(_expectedJson));
        _identityCookie.Value.ToString().ShouldEqual(expectedBase64);
    }

    [Fact] void should_set_cookie_as_not_http_only() => _identityCookie.HttpOnly.ShouldBeFalse();

    [Fact] void should_set_cookie_as_secure_when_request_is_https() => _identityCookie.Secure.ShouldBeTrue();

    [Fact] void should_set_cookie_with_lax_same_site() => _identityCookie.SameSite.ShouldEqual(Microsoft.Net.Http.Headers.SameSiteMode.Lax);

    [Fact] void should_set_cookie_path_to_root() => _identityCookie.Path.ShouldEqual("/");
}