// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Workload;

public class when_the_positive_cache_is_full : Specification
{
    readonly given.workload_verification _fixture = new(capacity: 1);

    async Task Because()
    {
        var first = _fixture.Token(300);
        await _fixture.Verify(first);
        await _fixture.Verify(_fixture.Token(301));
        await _fixture.Verify(first);
    }

    [Fact] void should_evict_instead_of_growing_without_bound() => _fixture.Handler.Reviews.ShouldEqual(3);
    void Destroy() => _fixture.Dispose();
}
