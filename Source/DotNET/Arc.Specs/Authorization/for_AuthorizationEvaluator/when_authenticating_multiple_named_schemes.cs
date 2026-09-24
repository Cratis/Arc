// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator;

public class when_authenticating_multiple_named_schemes : Specification
{
    ClaimsPrincipal? _withOneSuccess;
    ClaimsPrincipal? _withBothSuccess;
    ClaimsPrincipal? _withNoSuccess;

    async Task Because()
    {
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        foreach (var name in new[] { "One", "Two" })
        {
            schemes.GetSchemeAsync(name).Returns(Task.FromResult<AuthenticationScheme?>(
                new AuthenticationScheme(name, name, typeof(IAuthenticationHandler))));
        }

        var first = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "first")], "One"));
        var second = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "second")], "Two"));
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "One").Returns(
            _ => Task.FromResult(AuthenticateResult.Fail("one unavailable")));
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "Two").Returns(
            _ => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(second, "Two"))));
        var context = Substitute.For<IHttpContextAccessor>();
        context.HttpContext.Returns(new DefaultHttpContext());
        await using var services = new ServiceCollection()
            .AddSingleton(schemes)
            .AddSingleton(authentication)
            .AddSingleton(context)
            .BuildServiceProvider();
        var runtime = new AspNetAuthorizationPolicyRuntime(new ArcAuthorizationPolicyRuntime([]));
        var resolution = await runtime.Resolve(
            [AuthorizationRequirement.FromAttribute(null, null, "One,Two")], services, CancellationToken.None);
        var defaultPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "default")], "Default"));
        _withOneSuccess = await resolution.SelectPrincipal(defaultPrincipal, services, CancellationToken.None);

        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "One").Returns(
            _ => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(first, "One"))));
        _withBothSuccess = await resolution.SelectPrincipal(defaultPrincipal, services, CancellationToken.None);

        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "One").Returns(
            _ => Task.FromResult(AuthenticateResult.Fail("one unavailable")));
        authentication.AuthenticateAsync(Arg.Any<HttpContext>(), "Two").Returns(
            _ => Task.FromResult(AuthenticateResult.Fail("two unavailable")));
        _withNoSuccess = await resolution.SelectPrincipal(defaultPrincipal, services, CancellationToken.None);
    }

    [Fact] void should_accept_the_successful_scheme_when_another_fails() => _withOneSuccess!.Identity!.Name.ShouldEqual("second");
    [Fact] void should_combine_both_successful_scheme_identities() => _withBothSuccess!.Identities.Select(identity => identity.Name).ShouldContainOnly(["first", "second"]);
    [Fact] void should_not_reuse_the_default_identity_when_named_schemes_fail() => _withNoSuccess.ShouldBeNull();
}
