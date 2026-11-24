// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_reading_the_audiences : Specification
{
    ContextValueOutcome _outcome;
    int _calls;

    async Task Because()
    {
        var request = new ContextValueCatalog().CreateResolver(
            ContextScope.Application,
            null,
            _ =>
            {
                _calls++;
                return ValueTask.FromResult<EffectiveAudiences?>(Requests.Holding("Plus"));
            });
        await request.GetAudiences();
        _outcome = await request.Resolve("$context.audiences");
        await request.GetAudiences();
    }

    [Fact] void should_expose_them_under_the_context() => ((EffectiveAudiences)_outcome.Value!).Holds("Plus").ShouldBeTrue();
    [Fact] void should_resolve_them_once_per_request() => _calls.ShouldEqual(1);
}
