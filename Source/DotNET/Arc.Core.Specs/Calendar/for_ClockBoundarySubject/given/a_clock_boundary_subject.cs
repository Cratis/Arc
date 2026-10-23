// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject.given;

public class a_clock_boundary_subject : Specification
{
    protected static readonly DateTimeOffset Start = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    protected static readonly TenantId Tenant = new("acme");

    protected a_tracking_time_provider _time;
    protected Subject<int> _candidates;
    protected CancellationTokenSource _cancellation;
    protected ITenantScope _tenantScope;
    internal ClockBoundarySubject<int, int> _subject;
    protected Func<int, ClockReading, int> _compute;
    protected Func<ClockReading, DateTimeOffset?> _nextBoundary;
    protected List<int> _results;
    protected List<Exception> _errors;
    protected List<ClockReading> _readings;
    protected List<int> _scopesDuringEvaluation;
    protected List<TenantId> _begunTenants;
    protected IDisposable _subscription;
    protected int _completions;
    protected int _activeScopes;
    protected int _evaluations;

    protected IReadOnlyList<int> Results => _results;

    void Establish()
    {
        _time = new a_tracking_time_provider(Start);
        _candidates = new Subject<int>();
        _cancellation = new CancellationTokenSource();
        _results = [];
        _errors = [];
        _readings = [];
        _scopesDuringEvaluation = [];
        _begunTenants = [];
        _compute = (candidate, _) => candidate;
        _nextBoundary = _ => null;

        _tenantScope = Substitute.For<ITenantScope>();
        _tenantScope.Begin(Arg.Any<TenantId>()).Returns(callInfo =>
        {
            _begunTenants.Add(callInfo.Arg<TenantId>());
            _activeScopes++;
            return Disposable.Create(() => _activeScopes--);
        });

        Build(TimeZoneInfo.Utc);
    }

    void Destroy()
    {
        _subscription?.Dispose();
        _subject?.Dispose();
        _cancellation.Dispose();
        _candidates.Dispose();
    }

    protected void Build(TimeZoneInfo zone) =>
        _subject = new ClockBoundarySubject<int, int>(
            _candidates,
            new ClockBoundaryContext(Tenant, zone, _time, _tenantScope, new ChangeSetComputor(new JsonSerializerOptions()), _cancellation.Token),
            Evaluate);

    protected void Subscribe() =>
        _subscription = _subject.Subscribe(Observer.Create<int>(
            _results.Add,
            _errors.Add,
            () => _completions++));

    ClockEvaluation<int> Evaluate(int candidate, ClockReading reading)
    {
        _evaluations++;
        _readings.Add(reading);
        _scopesDuringEvaluation.Add(_activeScopes);
        return new ClockEvaluation<int>(_compute(candidate, reading), _nextBoundary(reading));
    }
}
