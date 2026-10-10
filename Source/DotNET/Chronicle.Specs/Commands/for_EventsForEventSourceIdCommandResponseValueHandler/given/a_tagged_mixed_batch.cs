// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.given;

public abstract class a_tagged_mixed_batch : Commands.given.a_named_tag_append
{
    protected override ICommandResponseValueHandler CreateHandler() => new EventsForEventSourceIdCommandResponseValueHandler(_eventLog, _eventTypes, _strategies);
    protected override object CreateValue() => new object[] { Wrapper(), new TestEvent("plain") };
}
