// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_InMemoryAudienceSource;

public class when_a_grant_changes : Specification
{
    InMemoryAudienceSource _source;
    List<AudienceChange> _changes;

    async Task Establish()
    {
        _source = new();
        _changes = [];
        _source.OnChanged(_ =>
        {
            _changes.Add(_);
            return ValueTask.CompletedTask;
        });
        await _source.Grant(AudienceLevel.Tenant, "t1", "Plus");
        await _source.Withdraw(AudienceLevel.Tenant, "t1", "Plus");
    }

    [Fact] void should_notify_for_both_changes() => _changes.Count.ShouldEqual(2);
    [Fact] void should_name_the_level_and_key() => _changes[0].ShouldEqual(new AudienceChange(AudienceLevel.Tenant, "t1"));
}
