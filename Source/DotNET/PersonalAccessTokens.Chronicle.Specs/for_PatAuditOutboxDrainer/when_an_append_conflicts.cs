// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Chronicle.for_PatAuditOutboxDrainer;

public class when_an_append_conflicts : given.an_audit_outbox
{
    async Task Establish()
    {
        _pending = [_issuance];
        await _drainer.Drain(TestContext.Current.CancellationToken);
        _acknowledged.Clear();
        _pending.Add(_issuance with { Metadata = _issuance.Metadata with { Scopes = ["changed"] } });
    }

    async Task Because() => _result = await _drainer.Drain(TestContext.Current.CancellationToken);

    [Fact] void should_leave_delivery_pending() => _result.ShouldBeFalse();
    [Fact] void should_not_acknowledge_the_conflicting_intent() => _acknowledged.ShouldBeEmpty();
    [Fact] async Task should_not_append_another_event() => (await Events()).Length.ShouldEqual(1);
}
