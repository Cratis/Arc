// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_checking_can_handle;

public class with_returned_event_tags : given.an_events_command_response_value_handler
{
    bool _canHandle;
    void Because() => _canHandle = _handler.CanHandle(_commandContext, new EventTags(new NamedTag("project", "one")));

    [Fact] void should_not_treat_tags_as_events() => _canHandle.ShouldBeFalse();
}
