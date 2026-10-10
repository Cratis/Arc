// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams.given;

public class a_period_completion : Specification
{
    protected CommandScenario<CompletePeriod> _scenario;
    protected EventSourceId _id;
    protected CommandResult _result;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new();
    }

    protected Task AssertCompleted() => _scenario.ShouldHaveCompletedStream("completion", "period");
    protected Task AssertAppended() => _scenario.ShouldHaveAppendedEvent<CompletePeriod, PeriodCompleted>(_id);

    void Destroy() => _scenario.Dispose();
}
