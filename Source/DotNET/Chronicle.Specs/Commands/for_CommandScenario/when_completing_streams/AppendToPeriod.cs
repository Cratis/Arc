// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

[Command]
[EventStreamType("completion")]
[EventStreamId("period")]
public record AppendToPeriod(EventSourceId Id)
{
    public PeriodCompleted Handle() => new();
}
