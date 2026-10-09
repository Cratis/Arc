// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

[Command]
[EventStreamType("completion")]
[EventStreamId("period")]
public record CompletePeriod(EventSourceId Id, bool CompletionFirst = false)
{
    public object Handle() => CompletionFirst
        ? (new CompleteStream(), new PeriodCompleted(), "response")
        : (new PeriodCompleted(), new CompleteStream(), "response");
}
