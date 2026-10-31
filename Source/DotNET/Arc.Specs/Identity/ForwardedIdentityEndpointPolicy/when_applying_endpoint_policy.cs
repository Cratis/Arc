// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_ForwardedIdentityEndpointPolicy;

public class when_applying_endpoint_policy : Specification
{
    DefaultHttpContext _anonymous;
    DefaultHttpContext _protected;
    DefaultHttpContext _forbidden;
    DefaultHttpContext _native;

    async Task Because()
    {
        _anonymous = await Apply(anonymous: true);
        _protected = await Apply();
        _forbidden = await Apply(valid: true, roles: "administrator");
        _native = await Apply(native: true);
    }

    [Fact] void should_allow_anonymous_access_with_invalid_forwarding() => _anonymous.Response.StatusCode.ShouldEqual(200);
    [Fact] void should_not_promote_the_anonymous_caller() => _anonymous.User.Identity.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_challenge_a_protected_request() => _protected.Response.StatusCode.ShouldEqual(401);
    [Fact] void should_forbid_a_valid_identity_without_the_role() => _forbidden.Response.StatusCode.ShouldEqual(403);
    [Fact] void should_preserve_native_authentication() => _native.Response.StatusCode.ShouldEqual(200);
    [Fact] void should_not_augment_native_claims() => _native.User.Claims.Select(claim => claim.Value).ShouldEqual(["native"]);
    [Fact] void should_retain_the_native_name() => _native.User.FindFirst(ClaimTypes.Name).Value.ShouldEqual("native");

    static async Task<DefaultHttpContext> Apply(bool anonymous = false, bool valid = false, string? roles = null, bool native = false)
    {
        var options = ForwardedIdentityForSpecs.Options();
        var monitor = Substitute.For<IOptionsMonitor<ArcOptions>>();
        monitor.CurrentValue.Returns(options);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(monitor);
        services.AddRouting();
        services.AddAuthorization();
        services.AddAuthentication(native ? "native" : MicrosoftIDentityPlatformAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(MicrosoftIDentityPlatformAuthHandler.SchemeName, _ => { })
            .AddScheme<AuthenticationSchemeOptions, ForwardedIdentityNativeAuthenticationHandler>("native", _ => { });
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = "POST";
        context.Request.PathBase = "/base";
        context.Request.Path = "/a%2Fb";
        context.Features.Get<IHttpRequestFeature>().RawTarget = "/base/a%2fb?x=1";
        var headers = ForwardedIdentityForSpecs.Headers();
        headers[ForwardedIdentityForSpecs.SignatureHeader] = valid ? ForwardedIdentityForSpecs.Sign(headers) : "invalid";
        foreach (var pair in headers)
        {
            context.Request.Headers[pair.Key] = pair.Value;
        }
        var metadata = new List<object> { new AuthorizeAttribute { Roles = roles } };
        if (anonymous)
        {
            metadata.Add(new AllowAnonymousAttribute());
        }
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "signed-specs"));
        var app = new ApplicationBuilder(provider);
        app.UseAuthentication();
        app.UseAuthorization();
        app.Run(_ => Task.CompletedTask);
        await app.Build()(context);
        return context;
    }
}
