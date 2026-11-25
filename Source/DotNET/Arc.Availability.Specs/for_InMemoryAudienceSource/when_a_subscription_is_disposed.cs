// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_InMemoryAudienceSource;

public class when_a_subscription_is_disposed : Specification
{
    InMemoryAudienceSource _source;
    int _calls;

    async Task Establish()
    {
        _source = new();
        var subscription = _source.OnChanged(_ =>
        {
            _calls++;
            return ValueTask.CompletedTask;
        });
        subscription.Dispose();
        await _source.Grant(AudienceLevel.System, null, "Plus");
    }

    [Fact] void should_not_notify() => _calls.ShouldEqual(0);
}
