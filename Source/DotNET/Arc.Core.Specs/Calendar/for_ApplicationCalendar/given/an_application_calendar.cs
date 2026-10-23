// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Calendar.for_ApplicationCalendar.given;

public class an_application_calendar : Specification
{
    protected static readonly DateTimeOffset Instant = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    protected string _applicationZone = "UTC";
    protected List<ICanProvideTenantCalendar> _providers;
    protected ApplicationCalendar _calendar;

    void Establish() => _providers = [];

    protected void CreateCalendar()
    {
        var instances = Substitute.For<IInstancesOf<ICanProvideTenantCalendar>>();
        instances.GetEnumerator().Returns(_ => _providers.GetEnumerator());
        var options = new ArcOptions { Calendar = new CalendarOptions { TimeZone = _applicationZone } };
        _calendar = new ApplicationCalendar(Options.Create(options), instances);
    }

    protected static ICanProvideTenantCalendar ProviderReturning(string? id)
    {
        var provider = Substitute.For<ICanProvideTenantCalendar>();
        provider.Provide(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<string?>(id));
        return provider;
    }
}
