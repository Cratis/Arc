// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_a_subsequent_request : given.a_resolver
{
    CallerResolution _next;

    async Task Because()
    {
        _result = await _resolver.GetCurrent();
        var nextRequest = NewRequest();
        _requests.Current.Returns(nextRequest);
        _next = await _resolver.GetCurrent();
    }

    [Fact] void should_not_leak_the_cached_resolution() => _next.ShouldNotBeSame(_result);
    [Fact] async Task should_verify_each_request() => await _source.Received(2).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
