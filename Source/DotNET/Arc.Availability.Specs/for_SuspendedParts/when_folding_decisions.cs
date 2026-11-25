// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.Suspensions.for_SuspendedParts;

public class when_folding_decisions : Specification
{
    static readonly DateTimeOffset _start = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    SuspendedParts _resumedLater;
    SuspendedParts _suspendedAgain;
    SuspendedParts _resumedAtAnotherLevel;
    SuspendedParts _resumedBeforeSuspending;

    void Because()
    {
        var part = typeof(given.an_availability_host.UngatedCommand);
        var path = PartPaths.For(part);

        _resumedLater = SuspendedParts.From([
            new(path, AudienceLevel.Tenant, true, _start),
            new(path, AudienceLevel.Tenant, false, _start.AddMinutes(1))]);
        _suspendedAgain = SuspendedParts.From([
            new(path, AudienceLevel.Tenant, false, _start.AddMinutes(1)),
            new(path, AudienceLevel.Tenant, true, _start.AddMinutes(2)),
            new(path, AudienceLevel.Tenant, true, _start)]);
        _resumedAtAnotherLevel = SuspendedParts.From([
            new(path, AudienceLevel.System, true, _start),
            new(path, AudienceLevel.Tenant, false, _start.AddMinutes(1))]);
        _resumedBeforeSuspending = SuspendedParts.From([
            new(path, AudienceLevel.User, false, _start),
            new(path, AudienceLevel.User, true, _start.AddMinutes(1))]);
    }

    [Fact] void should_let_the_newest_decision_win_when_it_is_a_resume() => _resumedLater.Covers(typeof(given.an_availability_host.UngatedCommand)).ShouldBeFalse();
    [Fact] void should_let_the_newest_decision_win_when_it_is_a_suspension() => _suspendedAgain.Covers(typeof(given.an_availability_host.UngatedCommand)).ShouldBeTrue();
    [Fact] void should_let_a_suspension_at_any_level_win() => _resumedAtAnotherLevel.Covers(typeof(given.an_availability_host.UngatedCommand)).ShouldBeTrue();
    [Fact] void should_suspend_when_the_suspension_is_the_newest() => _resumedBeforeSuspending.Covers(typeof(given.an_availability_host.UngatedCommand)).ShouldBeTrue();
}
