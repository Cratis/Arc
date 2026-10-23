// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Time.Testing;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// In America/St_Johns on 1987-10-25 the local date becomes Oct 25 at 02:30Z, returns to Oct 24 at 02:31Z and becomes
/// Oct 25 again at 03:30Z, so the answer must follow the date forward, back, then forward.
/// </summary>
public class when_reevaluating_across_a_backward_date_change : Specification
{
    static readonly DateTimeOffset _start = new(1987, 10, 25, 2, 0, 0, TimeSpan.Zero);
    static readonly DateOnly[] _items = [new(1987, 10, 24), new(1987, 10, 25)];

    FakeTimeProvider _time;
    Subject<DateOnly[]> _candidates;
    ClockBoundarySubject<DateOnly[], DateOnly[]> _subject;
    IDisposable _subscription;
    List<int> _counts;

    void Establish()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/St_Johns");
        _time = new FakeTimeProvider(_start);
        _candidates = new Subject<DateOnly[]>();
        _counts = [];
        _subject = new ClockBoundarySubject<DateOnly[], DateOnly[]>(
            _candidates,
            new ClockBoundaryContext(new TenantId("acme"), zone, _time, Substitute.For<ITenantScope>(), new ChangeSetComputor(new JsonSerializerOptions()), CancellationToken.None),
            (items, reading) => new ClockEvaluation<DateOnly[]>(
                [.. items.Where(item => item <= reading.Today)],
                LocalDateChanges.NextLocalDateChange(reading.Zone, reading.Instant)));
        _subscription = _subject.Subscribe(Observer.Create<DateOnly[]>(result => _counts.Add(result.Length)));
        _candidates.OnNext(_items);
    }

    void Because()
    {
        _time.Advance(TimeSpan.FromMinutes(30));
        _time.Advance(TimeSpan.FromMinutes(1));
        _time.Advance(TimeSpan.FromMinutes(59));
    }

    void Destroy()
    {
        _subscription.Dispose();
        _subject.Dispose();
        _candidates.Dispose();
    }

    [Fact] void should_follow_the_date_forward_back_then_forward() => _counts.ShouldContainOnly(1, 2, 1, 2);
}
