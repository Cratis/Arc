---
title: "Cratis Arc application calendar: use business dates"
description: Use the Cratis Arc application calendar to resolve local dates in an IANA time zone and re-evaluate live query results when a clock boundary passes.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Use Cratis Arc's application calendar when a command or query needs the date in a particular time zone. It pairs an instant, a point on the UTC timeline, with the zone that applies to the operation. Your code can then ask for the local date, for example to decide which reservations are due today, without reading the clock itself.

## Configure the application time zone

Set the zone under `Cratis:Arc:Calendar:TimeZone`, or in code:

```json
{
  "Cratis": {
    "Arc": {
      "Calendar": {
        "TimeZone": "Europe/Oslo"
      }
    }
  }
}
```

The default is `UTC`. Use an IANA time-zone identifier such as `Europe/Oslo`, so date calculations can follow that zone's daylight-saving rules. At host startup, an unknown id, a Windows-only id such as `W. Europe Standard Time`, or a non-zero fixed-offset id such as `Etc/GMT+5` throws `TimeZoneIsNotAnIanaZone`, which names the id. Arc does not substitute UTC for an invalid zone. A zone set in code is applied after configuration binding and wins over the bound value.

## Zone precedence

`IApplicationCalendar.ResolveZone` picks the first zone that exists:

1. The zone the operation declares.
2. The tenant's zone, from the first registered `ICanProvideTenantCalendar` that returns an id.
3. The application zone.

Arc does not select a zone from request headers or query parameters. If tenants use different zones, register an `ICanProvideTenantCalendar` like the one below. Returning an unusable id throws `TimeZoneIsNotAnIanaZone`; an exception from the provider propagates to the caller.

```csharp
using Cratis.Arc.Calendar;
using Cratis.Arc.Tenancy;

public class TenantCalendarFromSettings : ICanProvideTenantCalendar
{
    public ValueTask<string?> Provide(TenantId tenant, CancellationToken token) =>
        new(tenant.Value == "acme" ? "Asia/Tokyo" : null);
}
```

## Read an instant

`IApplicationCalendar.Read` returns a `ClockReading`. It never reads a clock; you pass the instant.

```csharp
ClockReading reading = await calendar.Read(DateTimeOffset.UtcNow, tenant, null, token);
DateOnly today = reading.Today;
DateOnly yesterday = reading.Today.AddDays(-1);
DateOnly sameInstantYesterday = reading.DateOf(reading.Instant.AddDays(-1));
```

`Today.AddDays(-1)` steps calendar days. `DateOf(Instant.AddDays(-1))` steps elapsed time, which differs across a daylight-saving change.

## Re-evaluate at clock boundaries

A live list of "items due today" needs to change at the next local date, even if no stored data changes. `ReevaluateAtBoundaries` takes a subject of candidates and re-evaluates the latest candidates whenever they change or a boundary passes. Return both the answer and its next clock boundary from your evaluation:

```csharp
var context = await clockBoundaries.Begin(tenant, null, token);

ISubject<IEnumerable<Item>> answer = candidates.ReevaluateAtBoundaries(
    context,
    (items, reading) => new ClockEvaluation<IEnumerable<Item>>(
        [.. items.Where(item => item.Due <= reading.Today)],
        LocalDateChanges.NextLocalDateChange(reading.Zone, reading.Instant)));
```

`LocalDateChanges.NextLocalDateChange` handles a local midnight, a gap that skips midnight, a wholly skipped date, and a transition that moves the local date back.

The next boundary of an evaluation must be after `reading.Instant`, or `null` when the result cannot change because of the clock. A boundary that is not after it ends the subject with a `ClockBoundaryIsNotInTheFuture` error instead of re-evaluating in a loop.

An evaluation whose result equals the last emitted result is not emitted, but only inside this operator. The first evaluation is always emitted. Collections are compared positionally, so a reorder is a change. Evaluations run one at a time inside the tenant of the context.

The subject connects on its first subscription. It releases its timer and its subscription to the candidates when the last subscriber leaves, the context's token is cancelled, or the stream errors or completes. Calling `OnNext` on it is not supported.

## Subscription lifetime

The zone is resolved once, when `IClockBoundaries.Begin` runs, and is fixed for the subscription. Changing the application zone needs a restart, and a changed tenant zone applies to new subscriptions.

## Test with a fake clock

The operator arms its timers on the registered `TimeProvider`, so a test can use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` and `Advance` past a boundary.

## Platform prerequisites

Zone resolution uses the host's time-zone data. Containers need `tzdata`, and the host must not run in a configuration that lacks time-zone data. In invariant-globalization mode zone ids still resolve, but verify that your image carries the data. A host without it fails at startup rather than falling back.

## Out of scope

This feature does not supply a continuously ticking `now` or an advanceable clock for query scenarios. For the connection and delivery behavior around a live answer, continue with [observable queries](queries/model-bound/observable-queries.md).
