// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates;

public class TestAggregateRoot : AggregateRoot
{
    public FirstEventType FirstEventTypeInstance;
    public SecondEventType SecondEventTypeInstance;

    public void OnFirstEvent(FirstEventType @event)
    {
        FirstEventTypeInstance = @event;
    }

    public void OnSecondEvent(SecondEventType @event, EventContext context)
    {
        SecondEventTypeInstance = @event;
    }

    public int OnActivateCount;

    public void ReportFailed(string message, ValidationResultSeverity severity = ValidationResultSeverity.Error) =>
        Failed(message, severity);

    protected override Task OnActivate()
    {
        OnActivateCount++;
        return Task.CompletedTask;
    }
}
