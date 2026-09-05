// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_handler_returns_enumerable_of_events_for_event_source_id;

public class and_command_is_executed : Specification
{
    CommandScenario<BroadcastNotificationCommand> _scenario;
    CommandResult _result;
    EventSourceId _firstRecipientId;
    EventSourceId _secondRecipientId;

    void Establish()
    {
        _firstRecipientId = EventSourceId.New();
        _secondRecipientId = EventSourceId.New();
        _scenario = new CommandScenario<BroadcastNotificationCommand>();
    }

    async Task Because() =>
        _result = await _scenario.Execute(new BroadcastNotificationCommand(
            [_firstRecipientId, _secondRecipientId],
            "Hello!"));

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_have_appended_notification_for_first_recipient() =>
        await _scenario.ShouldHaveAppendedEvent<BroadcastNotificationCommand, NotificationSent>(_firstRecipientId);
    [Fact] async Task should_have_appended_notification_for_second_recipient() =>
        await _scenario.ShouldHaveAppendedEvent<BroadcastNotificationCommand, NotificationSent>(_secondRecipientId);
    [Fact] async Task should_have_sent_correct_message_to_first_recipient() =>
        await _scenario.ShouldHaveAppendedEvent<BroadcastNotificationCommand, NotificationSent>(_firstRecipientId, e => e.Message == "Hello!");
    [Fact] async Task should_have_sent_correct_message_to_second_recipient() =>
        await _scenario.ShouldHaveAppendedEvent<BroadcastNotificationCommand, NotificationSent>(_secondRecipientId, e => e.Message == "Hello!");
    [Fact] async Task should_have_appended_two_events() =>
        await _scenario.ShouldHaveTailSequenceNumber<BroadcastNotificationCommand>(1ul);
}
