// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using System.Text;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.Callers.Http.for_VerifiedCallerResolver.when_resolving;

public class with_untrusted_tool_metadata : Specification
{
    CallerResolution _resolution;
    HttpStatusCode _status;
    ClaimsPrincipal _user;
    ClaimsPrincipal _resolvedUser;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.MapPost("/mcp", async http =>
        {
            _user = new ClaimsPrincipal(new ClaimsIdentity([new("sub", "forged-person"), new("scope", "fixture:admin")], "synthetic"));
            http.User = _user;
            var requests = Substitute.For<IHttpRequestContextAccessor>();
            requests.Current.Returns(new AspNetCoreHttpRequestContext(http));
            IVerifiedCallerAccessor callers = new VerifiedCallerResolver(requests, [], new(), new(new(new Dictionary<string, string>())), new(null, null), Substitute.For<ITenantIdAccessor>());
            _resolution = await callers.GetCurrent();
            _resolvedUser = http.User;
            http.Response.StatusCode = _resolution.Caller is null ? 401 : 200;
        });
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp?grant=forged-grant&client_id=forged-client");
            request.Headers.Add("Authorization", "Bearer synthetic-fixture");
            request.Headers.Add("X-Person", "forged-person");
            request.Headers.Add("X-Actor", "forged-actor");
            request.Headers.Add("X-Scopes", "fixture:admin");
            request.Content = new StringContent("{\"person\":\"forged-person\",\"grant\":\"forged-grant\",\"scope\":\"fixture:admin\"}", Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            _status = response.StatusCode;
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact] void should_fail_closed_at_the_http_tool_boundary() => _status.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_require_a_trusted_source() => _resolution.Refusal.ShouldEqual(CallerRefusal.NoMatchingSource);
    [Fact] void should_not_mint_a_caller_from_claims_headers_query_or_body() => _resolution.Caller.ShouldBeNull();
    [Fact] void should_preserve_the_request_principal_instance() => ReferenceEquals(_resolvedUser, _user).ShouldBeTrue();
    [Fact] void should_preserve_the_request_principal_subject() => _resolvedUser.FindFirst("sub")!.Value.ShouldEqual("forged-person");
    [Fact] void should_preserve_the_request_principal_scope() => _resolvedUser.FindFirst("scope")!.Value.ShouldEqual("fixture:admin");
    [Fact] void should_not_add_claims_to_the_request_principal() => _resolvedUser.Claims.Count().ShouldEqual(2);
}
