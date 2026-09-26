// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Identity;
using Cratis.Traces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_request;

/// <summary>
/// The <c>/.cratis/me</c> handler marks the response as not storable on entry and the identity provider marks it again
/// when it writes the identity cookie. Both calls land on the same ASP.NET Core response, which a CORS middleware may
/// already have given <c>Vary: Origin</c>.
/// </summary>
public class with_an_asp_net_core_response_varying_on_origin : Specification
{
    DefaultHttpContext _httpContext;
    AspNetCoreHttpRequestContext _context;
    Func<IHttpRequestContext, Task> _handler;
    System.Diagnostics.ActivitySource _activitySource;

    void Establish()
    {
        var options = new ArcOptions();
        var identity = new IdentityProviderResult(
            new IdentityId("test-id"),
            new IdentityName("Test User"),
            IsAuthenticated: true,
            IsAuthorized: true,
            Roles: [],
            "Additional details");
        var cookie = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(identity, options.JsonSerializerOptions)));

        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Headers.Cookie = $"{IdentityProvider.IdentityCookieName}={cookie}";
        _httpContext.Response.Body = new MemoryStream();
        _httpContext.Response.Headers.Vary = "Origin";
        _context = new AspNetCoreHttpRequestContext(_httpContext);

        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        accessor.Current.Returns(_context);
        var identityActivitySource = Substitute.For<IActivitySource<IdentityProvider>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Specs");
        identityActivitySource.ActualSource.Returns(_activitySource);
        var identityProvider = new IdentityProvider(accessor, Options.Create(options), identityActivitySource);
        _httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IIdentityProvider>(identityProvider)
            .BuildServiceProvider();

        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        serviceProviderIsService.IsService(typeof(IProvideIdentityDetails)).Returns(true);
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceProviderIsService)).Returns(serviceProviderIsService);

        var mapper = Substitute.For<IEndpointMapper>();
        mapper
            .When(_ => _.MapGet("/.cratis/me", Arg.Any<Func<IHttpRequestContext, Task>>(), Arg.Any<EndpointMetadata>()))
            .Do(call => _handler = call.ArgAt<Func<IHttpRequestContext, Task>>(1));
        mapper.MapIdentityProviderEndpoint(serviceProvider);
    }

    async Task Because() => await _handler(_context);

    void Destroy() => _activitySource.Dispose();

    [Fact] void should_write_the_identity_cookie() => _httpContext.Response.Headers.SetCookie.ToString().ShouldContain(IdentityProvider.IdentityCookieName);
    [Fact] void should_keep_origin_and_add_cookie_only_once() => _httpContext.Response.Headers.Vary.ToString().ShouldEqual("Origin, Cookie");
    [Fact] void should_set_no_store_cache_control() => _httpContext.Response.Headers.CacheControl.ToString().ShouldEqual("no-store, private");
    [Fact] void should_respond_with_ok() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
}
