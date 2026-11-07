// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Arc.PersonalAccessTokens.Chronicle.for_PatAuditOutboxDrainer;

public class when_idempotent_appends_are_unsupported : given.an_audit_outbox
{
    async Task Because()
    {
        var capabilities = new KernelCapabilitiesForTesting();
        capabilities.Old();
        var store = new EventStoreForTesting(new DefaultServiceProvider(), Defaults.Instance.ClientArtifactsProvider, new ChronicleOptions(), new ScenarioTime(IssuedAt), capabilities);
        _result = await new PatAuditOutboxDrainer(_outbox, store.EventLog).Drain(TestContext.Current.CancellationToken);
    }

    [Fact] void should_leave_delivery_pending() => _result.ShouldBeFalse();
    [Fact] void should_not_acknowledge_any_intent() => _acknowledged.ShouldBeEmpty();
    [Fact] void should_not_read_a_batch() => _outbox.DidNotReceive().ReadPending(Arg.Any<CancellationToken>());
}
