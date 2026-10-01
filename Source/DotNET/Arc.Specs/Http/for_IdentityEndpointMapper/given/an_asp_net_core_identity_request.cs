// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Identity;
using Cratis.Traces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.given;

/// <summary>
/// A request to <c>/.cratis/me</c> handled by the real <see cref="IdentityProvider"/> on an ASP.NET Core response.
/// </summary>
public class an_asp_net_core_identity_request : Specification
{
    protected const string ForgedIdentityCookieName = ".cratis-identity";

    protected DefaultHttpContext _httpContext;
    protected AspNetCoreHttpRequestContext _context;
    protected Func<IHttpRequestContext, Task> _handler;
    protected IProvideIdentityDetails _identityDetailsProvider;
    protected ArcOptions _options = new();
    System.Diagnostics.ActivitySource _activitySource;

    protected string ResponseBody => Encoding.UTF8.GetString(((MemoryStream)_httpContext.Response.Body).ToArray());

    void Establish()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
        _context = new AspNetCoreHttpRequestContext(_httpContext);

        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        accessor.Current.Returns(_context);
        var identityActivitySource = Substitute.For<IActivitySource<IdentityProvider>>();
        _activitySource = new System.Diagnostics.ActivitySource("Cratis.Arc.Specs");
        identityActivitySource.ActualSource.Returns(_activitySource);
        var identityProvider = new IdentityProvider(accessor, Options.Create(_options), identityActivitySource);
        _identityDetailsProvider = Substitute.For<IProvideIdentityDetails>();
        _identityDetailsProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(new IdentityDetails(true, new { department = "Engineering" }));
        _httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IIdentityProvider>(identityProvider)
            .AddSingleton(_identityDetailsProvider)
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

    void Destroy() => _activitySource.Dispose();

    protected void AuthenticateAs(string id, string name, params string[] roles)
    {
        var claims = new List<Claim> { new("sub", id), new(ClaimTypes.Name, name) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    protected void SendForgedIdentityCookie()
    {
        var forged = new IdentityProviderResult(
            new IdentityId("admin"),
            new IdentityName("Forged Administrator"),
            IsAuthenticated: true,
            IsAuthorized: true,
            Roles: ["admin"],
            Details: new { department = "Forged" });
        var cookie = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(forged, _options.JsonSerializerOptions)));
        _httpContext.Request.Headers.Cookie = $"{ForgedIdentityCookieName}={cookie}";
    }
}
