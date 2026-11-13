// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace OAuthPatCallerAcceptance.Specs;

public class when_a_signed_envelope_is_replayed : given.a_composed_host
{
    async Task Because()
    {
        await Start();
        var token = await Token();
        foreach (var path in new[] { "/rest", "/mcp" })
        {
            _envelope = _envelope with { Nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_') };
            var method = path == "/mcp" ? HttpMethod.Post : HttpMethod.Get;
            await Send(path, method, token);
            await Send(path, method, token);
        }
    }

    [Fact] void should_accept_only_the_first_presentation_per_transport() => _outcomes.Select(outcome => outcome.Accepted).ShouldEqual(new[] { true, false, true, false });
    [Fact] void should_refuse_each_replay() => _outcomes.Where((_, index) => index % 2 == 1).All(outcome => outcome.Status == HttpStatusCode.Unauthorized && outcome.SafeRefusal).ShouldBeTrue();
    [Fact] void should_dispatch_only_originals() => _store.Dispatches.ShouldEqual(2);
    [Fact] void should_use_only_the_selected_verifier_before_nonce_reservation() => _store.OAuthReads.ShouldEqual(4);
    [Fact] void should_not_fall_back() => _store.PatReads.ShouldEqual(0);
}
