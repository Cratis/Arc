// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using FluentValidation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

[Command]
public record ApproveTimesheet(TimesheetId Id)
{
    public TimesheetApproved Handle() => new();
}

[Command]
public record StartTimesheetReview(TimesheetId Id)
{
    public TimesheetReviewStarted Handle() => new();
}

public record TimesheetId(Guid Value) : EventSourceId<Guid>(Value);

public interface ITimesheetState
{
    TimesheetPhase Phase { get; }
}

public enum TimesheetPhase
{
    Draft = 0,
    Submitted = 1
}

public record TimesheetState(TimesheetPhase Phase) : ITimesheetState;

public class ApproveTimesheetValidator : CommandValidator<ApproveTimesheet>
{
    public ApproveTimesheetValidator(TimesheetState state)
    {
        RuleFor(command => command.Id)
            .Must(_ => state.Phase == TimesheetPhase.Submitted)
            .WithMessage("Timesheet must be submitted before it can be approved.");
    }
}

public class StartTimesheetReviewValidator : CommandValidator<StartTimesheetReview>
{
    public StartTimesheetReviewValidator(ITimesheetState state)
    {
        RuleFor(command => command.Id)
            .Must(_ => state.Phase == TimesheetPhase.Submitted)
            .WithMessage("Timesheet must be submitted before review can start.");
    }
}

[EventType("53fc1ff0-8ee9-4de0-8e2c-5d2742c3568f")]
public record TimesheetApproved;

[EventType("cfc55948-829d-4ca6-a733-56bb742e4f60")]
public record TimesheetReviewStarted;
