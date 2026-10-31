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
        var wrapper = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest>();
        wrapper.Items.Returns(_context.Items);
        wrapper.Headers.Returns(_context.Headers);
        wrapper.Method.Returns(_context.Method);
        wrapper.Path.Returns(_context.Path);
        var raw = (ICanReadForwardedIdentityRequest)wrapper;
        raw.HeaderNames.Returns(_rawHeaders.Keys);
        raw.RawTarget.Returns(((ICanReadForwardedIdentityRequest)_context).RawTarget);
        raw.GetHeaderValues(Arg.Any<string>()).Returns(call => _rawHeaders.TryGetValue(call.Arg<string>(), out var values) ? values : []);
        _results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => ForwardedIdentityAuthentication.Authenticate(wrapper, _options, "specs", wrapper.Path, _clock, _replays))));
        ((ClaimsIdentity)first.Principal!.Identity!).AddClaim(new Claim("injected", "yes"));
        _afterMutation = Authenticate();
    }

    [Fact] void should_share_the_complete_success_between_wrappers() => _results.All(result => result.Principal?.Identity?.IsAuthenticated == true).ShouldBeTrue();
    [Fact] void should_not_share_mutable_principals() => _results.Select(result => result.Principal).Distinct().Count().ShouldEqual(16);
    [Fact] void should_not_reuse_mutated_returned_claims() => _afterMutation.Principal!.HasClaim(claim => claim.Type == "injected").ShouldBeFalse();
}
