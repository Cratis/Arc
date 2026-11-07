// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Chronicle.for_PatAuditOutboxDrainer;

public class when_redelivering_after_acknowledgement_loss : given.an_audit_outbox
{
    Exception? _firstFailure;

    async Task Establish()
    {
        _pending = [_issuance];
        _outbox.Acknowledge(_issuance.Id, Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("Acknowledgement unavailable.")));
        try
        {
            await _drainer.Drain(TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            _firstFailure = exception;
        }
        _outbox.Acknowledge(_issuance.Id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    async Task Because() => _result = await new PatAuditOutboxDrainer(_outbox, _store.EventLog).Drain(TestContext.Current.CancellationToken);

    [Fact] void should_observe_the_lost_acknowledgement() => _firstFailure.ShouldNotBeNull();
    [Fact] void should_succeed_on_retry() => _result.ShouldBeTrue();
    [Fact] async Task should_append_only_one_event() => (await Events()).Length.ShouldEqual(1);
}
