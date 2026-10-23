---
title: Application calendar
description: Resolve the time zone that applies to an operation and re-evaluate live answers when the local date changes.
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

The application calendar answers one question: which time zone's calendar applies to this operation? It pairs an instant you supply with that zone, so code can ask for the local date without reading the clock itself.

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

The default is `UTC`. Only IANA ids are accepted. When the host starts, an unknown id, a Windows-only id such as `W. Europe Standard Time`, and a non-zero fixed-offset id such as `Etc/GMT+5` all throw `TimeZoneIsNotAnIanaZone`, which names the id. Arc never replaces a zone it cannot resolve with UTC, because that would move every local date without anyone noticing. A zone set in code is applied after configuration binding, so it wins over the bound value.

## Zone precedence

`IApplicationCalendar.ResolveZone` picks the first zone that exists:

1. The zone the operation declares.
2. The tenant's zone, from the first registered `ICanProvideTenantCalendar` that returns an id.
3. The application zone.

Only the operation, a provider you register, and the configuration can choose a zone. A request header or query parameter never does. A provider that returns an unusable id throws `TimeZoneIsNotAnIanaZone`, and a provider that throws propagates the exception.

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

An answer such as "items due today" changes when the local date changes, even though no data changed. `ReevaluateAtBoundaries` takes a subject of candidates and re-evaluates the latest candidates whenever they change and whenever a boundary passes.

```csharp
var context = await clockBoundaries.Begin(tenant, null, token);

ISubject<IEnumerable<Item>> answer = candidates.ReevaluateAtBoundaries(
    context,
    (items, reading) => new ClockEvaluation<IEnumerable<Item>>(
        [.. items.Where(item => item.Due <= reading.Today)],
        LocalDateChanges.NextLocalDateChange(reading.Zone, reading.Instant)));
```

`LocalDateChanges.NextLocalDateChange` handles a local midnight, a gap that skips midnight, a wholly skipped date, and a transition that moves the local date back.

An evaluation whose result equals the last emitted result is not emitted, but only inside this operator. The first evaluation is always emitted. Collections are compared positionally, so a reorder is a change. Evaluations run one at a time inside the tenant of the context.

The subject connects on its first subscription. It releases its timer and its subscription to the candidates when the last subscriber leaves, the context's token is cancelled, or the stream errors or completes. Calling `OnNext` on it is not supported.

## Subscription lifetime

The zone is resolved once, when `IClockBoundaries.Begin` runs, and is fixed for the subscription. Changing the application zone needs a restart, and a changed tenant zone applies to new subscriptions.

## Test with a fake clock

The operator arms its timers on the registered `TimeProvider`, so a test can use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` and `Advance` past a boundary.

## Platform prerequisites

Zone resolution uses the host's time-zone data. Containers need `tzdata`, and the host must not run in a configuration that lacks time-zone data. In invariant-globalization mode zone ids still resolve, but verify that your image carries the data. A host without it fails at startup rather than falling back.

## Out of scope

A live `now` that ticks continuously, and an advanceable clock for query scenarios, are not part of this feature.
