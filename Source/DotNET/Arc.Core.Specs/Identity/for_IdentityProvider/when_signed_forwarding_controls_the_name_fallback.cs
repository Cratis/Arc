// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_IdentityProvider;

public class when_signed_forwarding_controls_the_name_fallback : for_ForwardedIdentityAuthentication.given.a_signed_request
{
    IdentityProviderResult _legacy;
    IdentityProviderResult _native;
    IdentityProviderResult _signed;
    IdentityProviderResult _anonymous;

    async Task Because()
    {
        var details = Substitute.For<IProvideIdentityDetails>();
        details.Provide(Arg.Any<IdentityProviderContext>()).Returns(new IdentityDetails(true, new object()));
        await using var services = new ServiceCollection().AddSingleton(details).BuildServiceProvider();
        _context.RequestServices.Returns(services);
        var accessor = Substitute.For<IHttpRequestContextAccessor>();
        accessor.Current.Returns(_context);
        using var source = new ActivitySource("signed-forwarding-specs");
        var traces = Substitute.For<IActivitySource<IdentityProvider>>();
        traces.ActualSource.Returns(source);
        var provider = new IdentityProvider(accessor, Options.Create(_options), traces);
        _context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "native")], "native"));
        _headers[MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "unverified-name";
        _options.ForwardedIdentity.Enabled = false;
        _legacy = await provider.Get();
        _options.ForwardedIdentity.Enabled = true;
        _native = await provider.Get();
        _context.User = Authenticate().Principal;
        _signed = await provider.Get();
        _context.User = new ClaimsPrincipal();
        _anonymous = await provider.Get();
    }

    [Fact] void should_preserve_the_legacy_name_fallback_when_off() => _legacy.Name.Value.ShouldEqual("unverified-name");
    [Fact] void should_not_add_an_unverified_name_to_native_identity_when_on() => _native.Name.Value.ShouldEqual("unknown");
    [Fact] void should_preserve_the_native_subject() => _native.Id.Value.ShouldEqual("native");
    [Fact] void should_use_the_verified_principal_name() => _signed.Name.Value.ShouldEqual("n");
    [Fact] void should_not_promote_anonymous_identity_from_raw_headers() => _anonymous.IsAuthenticated.ShouldBeFalse();
}
