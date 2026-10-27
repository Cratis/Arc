// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Identities.for_ArcIdentityProvider.when_appending;

public class with_an_on_behalf_forward : given.an_on_behalf_request_appending_to_chronicle
{
    AppendResult _result;
    EventMetadata? _metadata;

    async Task Because()
    {
        _result = await _eventLog.Append("ticket-1", new given.TicketClosed("Resolved by support"));
        _metadata = await _store.EventLog.GetMetadataAt(_result.SequenceNumber);
    }

    [Fact] void should_authenticate_the_forward() => _authentication.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_append_the_event() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_record_the_actor_as_the_cause() => _metadata!.CausedBy.Subject.ShouldEqual(ActorId);
    [Fact] void should_record_the_represented_user_as_the_one_acted_on_behalf_of() => _metadata!.CausedBy.OnBehalfOf!.Subject.ShouldEqual(RepresentedId);
    [Fact] void should_end_the_chain_with_the_represented_user() => _metadata!.CausedBy.OnBehalfOf!.OnBehalfOf.ShouldBeNull();
    [Fact] void should_classify_the_initiator_as_an_agent() => _metadata!.InitiatorType.ShouldEqual(InitiatorType.Agent);
}
