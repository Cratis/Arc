// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

[Command]
public record CompleteDefaultStream(EventSourceId Id)
{
    public (PeriodCompleted, CompleteStream) Handle() => (new(), new());
}
