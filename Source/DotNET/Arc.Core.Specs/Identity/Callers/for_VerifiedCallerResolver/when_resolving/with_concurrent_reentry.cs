// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerResolver.when_resolving;

public class with_concurrent_reentry : given.a_resolver
{
    CallerResolution[] _results;

    async Task Because() => _results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(_resolver.GetCurrent)));

    [Fact] void should_reuse_the_same_resolution() => _results.All(result => ReferenceEquals(result, _results[0])).ShouldBeTrue();
    [Fact] async Task should_verify_only_once() => await _source.Received(1).Verify(Arg.Any<CredentialPresentation>(), Arg.Any<CancellationToken>());
}
