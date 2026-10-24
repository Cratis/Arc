// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// Every evaluation, timer epochs included, runs inside the subscription's tenant.
/// </summary>
public class when_evaluating_inside_the_tenant_scope : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (candidate, reading) => candidate + (int)(reading.Instant - Start).TotalHours;
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        Subscribe();
        _candidates.OnNext(1);
    }

    void Because() => _time.Advance(TimeSpan.FromHours(1));

    [Fact] void should_begin_the_tenant_for_every_evaluation() => _begunTenants.ShouldContainOnly(Tenant, Tenant);
    [Fact] void should_evaluate_while_the_scope_is_open() => _scopesDuringEvaluation.ShouldContainOnly(1, 1);
    [Fact] void should_end_the_scope_after_evaluating() => _activeScopes.ShouldEqual(0);
}
