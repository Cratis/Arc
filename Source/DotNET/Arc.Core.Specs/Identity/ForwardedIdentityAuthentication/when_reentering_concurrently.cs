// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication;

public class when_reentering_concurrently : given.a_signed_request
{
    ForwardedIdentityAuthenticationResult[] _results;
    ForwardedIdentityAuthenticationResult _afterMutation;

    async Task Because()
    {
        var first = Authenticate();

        // Capture before configuring another substitute: nested substitute calls would bind Returns to the wrong getter.
        var items = _context.Items;
        var headers = _context.Headers;
        var method = _context.Method;
        var path = _context.Path;
        var target = ((ICanReadForwardedIdentityRequest)_context).RawTarget;
        var wrapper = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest>();
        wrapper.Items.Returns(items);
        wrapper.Headers.Returns(headers);
        wrapper.Method.Returns(method);
        wrapper.Path.Returns(path);
        var raw = (ICanReadForwardedIdentityRequest)wrapper;
        raw.HeaderNames.Returns(_rawHeaders.Keys);
        raw.RawTarget.Returns(target);
        raw.GetHeaderValues(Arg.Any<string>()).Returns(call => _rawHeaders.TryGetValue(call.Arg<string>(), out var values) ? values : []);
        wrapper.Method.ShouldEqual("POST");
        wrapper.Path.ShouldEqual("/base/a%2Fb");
        raw.RawTarget.ShouldEqual("/base/a%2fb?x=1");
        ReferenceEquals(wrapper.Items, _context.Items).ShouldBeTrue();
        _results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => ForwardedIdentityAuthentication.Authenticate(wrapper, _options, "specs", wrapper.Path, _clock, _replays))));
        ((ClaimsIdentity)first.Principal!.Identity!).AddClaim(new Claim("injected", "yes"));
        _afterMutation = Authenticate();
    }

    [Fact] void should_share_the_complete_success_between_wrappers() => _results.All(result => result.Principal?.Identity?.IsAuthenticated == true).ShouldBeTrue();
    [Fact] void should_not_share_mutable_principals() => _results.Select(result => result.Principal).Distinct().Count().ShouldEqual(16);
    [Fact] void should_not_reuse_mutated_returned_claims() => _afterMutation.Principal!.HasClaim(claim => claim.Type == "injected").ShouldBeFalse();
}
