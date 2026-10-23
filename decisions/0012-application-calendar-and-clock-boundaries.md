---
id: 0012-application-calendar-and-clock-boundaries
title: Resolve an application calendar on the server and re-evaluate live queries at clock boundaries
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Arc.Core/ArcOptions.cs
  - Source/DotNET/Arc.Core/HostBuilderExtensions.cs
  - Source/DotNET/Arc.Core/Calendar/**
  - Source/DotNET/Arc.Core/Queries/**
  - Source/DotNET/Testing/**
  - Documentation/backend/csharp/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Proposed on 2026-10-23. Acceptance and a named human decider remain pending.

## Context

[Arc #3521](https://github.com/Cratis/Arc/issues/3521) asks for the runtime half of time-relative queries. Screenplay's
proposed records (local branch `screenplay-run3-decisions` at `a3b4d6fb`, not yet numbered) define the language half:
"The application calendar and clock operands `today` and `now`" (the calendar record) and "A live answer that reads the
clock follows the clock" (the live record). They amend Screenplay
[0087](https://github.com/Cratis/Screenplay/blob/main/decisions/0087-admit-chronological-date-and-datetime-ordering-in-command-conditions.md)
item 7 (`today` is the UTC date of the receipt instant) and supplement Screenplay
[0010](https://github.com/Cratis/Screenplay/blob/main/decisions/0010-query-paging-ordering-and-live-delivery.md).
The calendar record asks Arc for "an application time-zone abstraction with a default from options and a tenant hook,
plus the `TimeProvider` boundaries"; the live record leaves the calendar-change rule (its item 9) to Arc. The two drafts
agree on the runtime contract.

Checked on `origin/main` at `8330cfdbc62d4dcd98f8dd90a91a8df508cd942d`:

- **Receipt instant.** `QueryContext.ReceivedAt` (`Source/DotNET/Arc.Core/Queries/QueryContext.cs:29-34`) and
  `CommandContext.ReceivedAt` (`Source/DotNET/Arc.Core/Commands/CommandContext.cs:63`) are captured once per operation
  from the injected `TimeProvider` (`Source/DotNET/Arc.Core/OperationContextScope.cs:21-25`), registered as
  `TimeProvider.System` unless replaced (`Source/DotNET/Arc.Core/HostBuilderExtensions.cs:89`). A hand-constructed
  context has `default` (`QueryContext.cs:32`); authorization then falls back to the clock
  (`Source/DotNET/Arc.Core/Authorization/AuthorizationEvaluation.cs:221-226`).
- **No zone.** No `TimeZoneInfo`, IANA or NodaTime use exists in `Source/DotNET` outside specs. `Tenant` is
  `(Id, Name)` for development tooling (`Source/DotNET/Arc.Core/Tenancy/Tenant.cs:6-11`;
  `Source/DotNET/Arc.Core/Tenancy/ICanProvideTenants.cs:6-15`); `TenancyOptions` configures tenant resolution only
  (`Source/DotNET/Arc.Core/Tenancy/TenancyOptions.cs:9-62`). `UseInvariantCulture` clears request culture providers
  (`Source/DotNET/Arc/CultureWebApplicationBuilderExtensions.cs:27-41`).
- **Options.** `ArcOptions` binds from `Cratis:Arc` (`HostBuilderExtensions.cs:33`, `:51-52`), validates on start
  (`:54-55`) and applies code-supplied options with `PostConfigure` after binding (`:57-60`;
  `Source/DotNET/Arc/WebApplicationBuilderExtensions.cs:75`, `:82`). `TenancyOptionsValidator` is the pattern for
  failing misconfiguration at startup (`Source/DotNET/Arc.Core/Tenancy/TenancyOptionsValidator.cs:12-18`, `:27`).
- **Live queries.** A subscription runs the query pipeline once (`Source/DotNET/Arc.Core/Queries/ObservableQueryDemultiplexer.Subscriptions.cs:319`)
  and subscribes to the returned stream (`:350`); only `ISubject<>` and `IAsyncEnumerable<>` results stream (`:106-108`).
  Every stream emission passes the emission guards (`Source/DotNET/Arc.Core/Queries/ObservableQueryDemultiplexer.Emissions.cs:111-122`) and is
  delivered with its change set; an emission with an empty change set is not dropped (`:85-95`). Ambient
  `AsyncLocal` context does not flow into emissions, and direct transports do not serialize them
  (`Source/DotNET/Arc.Core/Queries/ObservableQueryEmissionContext.cs:18-26`). `ITenantScope.Begin` selects a tenant
  explicitly (`Source/DotNET/Arc.Core/Tenancy/ITenantScope.cs:9-23`). Nothing in `Source/DotNET` outside specs uses
  `Observable.Interval`, `Observable.Timer`, `PeriodicTimer` or `TimeProvider.CreateTimer`.
- **Compiled filters.** Chronicle's `Observe<T>(filter)` compiles the filter once per subscription and applies it when
  the collection emits (Chronicle `origin/main` `2ca74d540d`, `Source/Clients/DotNET/ReadModels/ReadModels.cs:299-322`;
  `IReadModels.cs:90`). Arc's MongoDB `Observe` likewise takes the filter once
  (`Source/DotNET/MongoDB/MongoCollectionExtensions.cs:55-57`). Midnight alone causes no emission.
- **Testing.** `ScenarioTimeProvider` fixes `GetUtcNow` only; timers stay real
  (`Source/DotNET/Testing/Commands/ScenarioTimeProvider.cs:9-17`;
  `Source/DotNET/Testing/Commands/CommandScenarioGeneratedValueExtensions.cs:56`).
- **Streaming shapes.** A subscription streams only when the query result implements `ISubject<>` or
  `IAsyncEnumerable<>`. Any other result, a plain `IObservable<>` included, is delivered once as a snapshot
  (`Source/DotNET/Arc.Core/Queries/ObservableQueryDemultiplexer.Subscriptions.cs:106-108`, `:339-343`).
- **Local dates can go back.** In `America/St_Johns` on 1987-10-25 the local date becomes Oct 25 at 02:30Z, returns
  to Oct 24 at 02:31Z (local 23:01, offset −02:30 to −03:30), and becomes Oct 25 again at 03:30Z (checked with
  Python `zoneinfo`). The local date can therefore change more than once in one night, and backwards. No `Microsoft.Extensions.TimeProvider.Testing` package is
  referenced (`Directory.Packages.props`).

## Decision

### 1. The application zone

`ArcOptions` gains `Calendar.TimeZone`: an IANA id, default `"UTC"`, bound from `Cratis:Arc:Calendar:TimeZone` like
other Arc options. Arc has no "configurable" flag. Stage enforces Screenplay's non-`configurable` declaration by
setting the zone in code, which `PostConfigure` applies after configuration binding, so host configuration cannot
change it. A `configurable` declaration renders the declared default and lets configuration override it.

A start-time validator resolves the id with `TimeZoneInfo.FindSystemTimeZoneById` and requires `HasIanaId`. An unknown
id, a Windows-only id, or a fixed offset stops the host with a typed exception that names the id. Arc never replaces
an invalid zone with UTC.

### 2. Resolution

A singleton `IApplicationCalendar` resolves one zone and pairs it with one instant (names not final):

```csharp
ValueTask<ClockReading> Read(DateTimeOffset instant, TenantId tenant, TimeZoneInfo? operationZone, CancellationToken token);
record ClockReading(DateTimeOffset Instant, TimeZoneInfo Zone);  // Today, DateOf(instant)
```

Precedence, first that applies: the operation's own `calendar` (`operationZone`); a tenant zone from an optional
host-registered `ICanProvideTenantCalendar` (`ValueTask<string?> Provide(TenantId, CancellationToken)`, null means
"none"); the application zone; UTC. An invalid tenant id fails the operation or subscription with the same typed
exception; it is never replaced.

- **Trusted sources only.** Arc reads a zone only from options, the tenant provider, or a `TimeZoneInfo` that
  generated code passes for a literal `calendar` or a host-supplied typed context member (Screenplay
  [0012](https://github.com/Cratis/Screenplay/blob/main/decisions/0012-typed-context-descriptor-and-command-handler-role.md)). Arc never
  derives a zone from headers, claims, culture, query arguments or command properties. Culture stays invariant.
- **One reading per evaluation.** `Read` takes the instant and never reads the clock. A snapshot query uses
  `QueryContext.ReceivedAt` and a command `CommandContext.ReceivedAt`; a reaction uses the occurrence instant of
  0087 item 7. The zone is resolved once per operation, after tenant resolution, and reused for that operation. A
  redelivered reaction resolves it again (the calendar record, item 4). `ReceivedAt` and `TimeProvider` keep their
  meaning; Arc adds no zone property to `QueryContext` or `CommandContext`.
- **Day and time arithmetic.** `Today` is the local date of `Instant` in `Zone`. The instant-to-local-date conversion
  is total, so daylight saving never makes it ambiguous. `today ± N days` is `DateOnly` arithmetic (calendar days);
  `now ± D` is `DateTimeOffset` arithmetic where a day is 24 hours. `DateOf(x)` converts a stored instant the same way.

### 3. The clock boundary primitive

Arc provides one `TimeProvider`-driven operator for streaming query results (names not final):

```csharp
ISubject<TResult> ReevaluateAtBoundaries<TSource, TResult>(
    this ISubject<TSource> candidates, ClockBoundaryContext context,
    Func<TSource, ClockReading, ClockEvaluation<TResult>> evaluate);
record ClockEvaluation<TResult>(TResult Result, DateTimeOffset? NextBoundary);
DateTimeOffset NextLocalDateChange(TimeZoneInfo zone, DateTimeOffset instant);
```

- **Result shape.** The operator returns `ISubject<TResult>`, a shape that subscriptions already stream
  (`IsStreamingResult`), so stream admission and the subscription path do not change. Its observer side does not forward to the
  candidates, because a `TResult` cannot become a `TSource`: `OnNext` from outside throws `NotSupportedException`,
  while `OnError` and `OnCompleted` terminate the output stream, cancel the boundary timer and dispose the candidate
  subscription. Arc's transports call `Subject.OnError` when a push fails
  (`Source/DotNET/Arc.Core/Queries/ClientObservable.cs:192,208`), so terminal calls must stay supported. A transport spec subscribes to a query that returns the operator
  over each Arc transport: WebSocket and server-sent events
  (`ObservableQueryDemultiplexer.WebSocket.cs`, `ObservableQueryDemultiplexer.Sse.cs` in `Source/DotNET/Arc.Core/Queries`). It
  asserts that a clock epoch arrives as a stream emission, not as a one-off snapshot.

- **Epochs.** On each candidate emission (a data epoch), the operator reads `TimeProvider.GetUtcNow()`, calls
  `evaluate` with the latest candidates and emits the result. It arms a one-shot `TimeProvider.CreateTimer` for
  `NextBoundary`; null arms nothing. When the timer fires (a clock epoch), it reads the clock again. If the instant
  is still before the boundary (early fire or the clock moved back), it re-arms and emits nothing. Otherwise it
  evaluates the last candidates at that instant, which is at least the boundary and is the current instant after a
  sleep, and arms the next boundary. Due times beyond the timer's maximum are re-armed in steps.
- **No-op suppression.** Within this operator, an epoch of either kind (data or clock) whose result equals the last
  emitted result is not emitted. This meets the live record's item 1: "A change set is sent only when the answer
  changed". Equality follows `ChangeSetComputor`'s identity-then-JSON rule
  (`Source/DotNET/Arc.Core/Queries/ChangeSetComputor.cs:10-27`). The first evaluation is always emitted. Streams that
  do not use the operator keep today's behaviour and emit every candidate emission.
- **`today`.** `NextLocalDateChange` returns the earliest instant strictly after `instant` at which the local date in
  `zone` differs from the local date at `instant`, forward or backward. Every zone has a next local midnight, so a
  result always exists. It covers every local-date transition, not just the start of the next
  date:
  - a local midnight;
  - a gap that skips midnight (the first valid instant of the new date);
  - a wholly skipped date (a jump of more than one date);
  - an offset transition that moves the local date back across midnight.

  It does this by taking the earlier of the next local midnight after `instant` and the next offset transition of
  `zone` that changes the local date; transitions that keep the date are stepped over. In the St John's vector the
  boundaries after 02:00Z are 02:30Z, 02:31Z and 03:30Z, then the next midnight. This tightens the live record's
  item 3, which schedules only "the start of the next local calendar date" and so would leave the answer wrong from
  02:31Z to 03:30Z.
- **`now`.** The operator accepts any next boundary, so next-flip support needs no new primitive. Arc does not ship
  a `now` next-flip helper (the live record's item 4 table) in the first increment. Consumers refuse live `now`
  conditions and serve them as snapshots until it exists.
- **Where it runs.** Inside the query's own stream. Arc does not re-run the pipeline at a boundary: that would bind
  and authorize again, capture a new `ReceivedAt` and reset the delta baseline. Clock-epoch results pass the same
  emission guards and change-set computation as data emissions.
- **Chronicle.** Clock predicates never go into Chronicle's `Observe(filter)` or MongoDB's `Observe` filter. Generated
  code observes the candidate set with only non-clock filters and applies the clock predicate in `evaluate`. Chronicle
  does not change.
- **Context and lifetime.** `ClockBoundaryContext` carries the tenant, the zone resolved at subscription and the
  cancellation token. The operator begins `ITenantScope` around `evaluate`, because timer callbacks carry no ambient
  context. It serializes data and clock epochs so `evaluate` never runs concurrently. It disposes its timer when the
  subscription ends, errors or completes. Snapshots arm no timer.
- **Tenants.** Each subscription arms its own timers in its own tenant's zone. A shared per-zone ticker is deferred
  until measurements show a need.
- **Calendar changes (the live record's item 9).** The zone is fixed for the subscription's lifetime. A changed
  application zone requires a restart, because options are bound at start. A changed tenant zone applies to new
  subscriptions and reconnects.
- **Testability.** The operator takes only the DI `TimeProvider`. Specs use `FakeTimeProvider`
  (`Microsoft.Extensions.TimeProvider.Testing`, added as a test-only package) to advance across boundaries.
  `ScenarioTimeProvider` cannot do this today.

### 4. Out of scope

User display time zones (Scene/Components), client evaluation of clock operands, Chronicle changes, durable scheduling
and clock reactions (Screenplay [0006](https://github.com/Cratis/Screenplay/blob/main/decisions/0006-reaction-triggers-declare-reads.md)
defers business scheduling), per-user business zones, Stage and Screenplay rendering, and batching subscribers.

## Options considered

- **Zone and boundary operator in Arc, explicit `Read` (chosen).** One path serves queries, commands, reactions and
  timer callbacks. It is additive, does not change `ReceivedAt`, and needs no Chronicle change.
- **A zone property on `QueryContext`/`CommandContext` (rejected).** Hand-built contexts would carry a default zone as
  silently as they carry a default `ReceivedAt`. Tenant lookup may be asynchronous, and timer callbacks have no
  operation context.
- **A zone on `Tenant` (rejected).** `Tenant` is a development-tooling record, and the zone would overload tenant
  identity. The optional provider keeps tenant zones opt-in.
- **Zone from request culture, a header or a claim (rejected).** The client would control a server decision, and two
  users would see different "overdue" sets.
- **NodaTime (rejected).** A new runtime dependency. `TimeZoneInfo` with IANA ids is enough given the platform
  prerequisites under Consequences.
- **Re-run the pipeline at boundaries (rejected).** It repeats binding and authorization, moves `ReceivedAt`,
  re-subscribes storage and resets delta state.
- **Rx `Observable.Timer` (rejected).** Rx timers run on an `IScheduler`, not the DI `TimeProvider`; bridging them adds
  an adapter and a second clock that tests must keep in step.
- **Client polling, synthetic events, Chronicle clock reactions, or a filter pushed into `Observe` (rejected).** The
  same reasons as the live record: client clock authority, derived history, and a filter that freezes.
- **Suppress every unchanged emission, data epochs included (rejected).** It changes current delivery behaviour.

## Open questions for the decider

1. **Tenant hook now or later.** Screenplay's open points 2 and 3 have not settled per-tenant zones. Recommendation:
   ship the optional provider now, because it is additive and unused unless registered. Alternative: application and
   operation zones only, with the hook added later.
2. **Calendar changes.** Recommendation: fixed per subscription (above). Alternative: an Arc change signal that starts
   a new epoch in affected subscriptions, at the cost of an extra API and invalidation fan-out.
3. **Live `now`.** Recommendation: refuse in the first increment. Alternative: ship the transition helper for the six
   comparisons with the operator.
4. **Id strictness.** Recommendation: IANA-only, so specifications and runtime agree across operating systems.
   Alternative: accept any id `FindSystemTimeZoneById` resolves.
5. **Query scenario support.** Should `Cratis.Arc.Testing` gain an advanceable clock for query scenarios now? The
   recommendation is to make it a follow-on.
6. **Names and namespace** (`Cratis.Arc.Calendar`, `IApplicationCalendar`, `ClockReading`, `ReevaluateAtBoundaries`).

## Default if unanswered

Arc ships nothing. Screenplay keeps `today` refused (`PLAY0268`). Applications that need a business day continue to
use hand-written filters that freeze at subscription, as SoloCRM does. Nothing is implied about other choices.

## Timeline and scope

Acceptance is required before Screenplay admits the calendar and live records, and before Stage renders against this
API. Implement in one minor Arc release: options and validator, `IApplicationCalendar`, tenant provider, the operator
and `NextLocalDateChange`, specs, and documentation under `Documentation/backend/csharp/`. This record creates no issue.

## Verification

- **Done when:** an invalid, Windows-only or offset zone stops the host; precedence and trusted-source rules hold;
  `Read` never reads the clock; live subscriptions re-evaluate at every local-date change and nowhere else, and emit only when the answer
  changes, without polling or
  events; unchanged epochs are suppressed only inside the operator; a
  transport spec shows operator results stream on every transport; timers are disposed with the subscription.
- **Verify by:** specs with `FakeTimeProvider` for UTC and a non-UTC zone. Cover a midnight in a gap, a wholly skipped
  date (for example `Pacific/Apia`, 2011-12-30), an ambiguous midnight, the backward date change in `America/St_Johns` on 1987-10-25 (boundaries
  02:30Z, 02:31Z, 03:30Z), early fire with a clock moved back, a sleep
  across several boundaries, and the timer-maximum step. Also cover tenant scope inside the callback, serialized
  epochs, disposal on cancel, error and complete, a failing tenant provider, and `today ± N` against `now - N days`
  across a DST change. Then run the Arc release build and spec gates, and a Stage and Arc conformance vector per the
  live record's item 8.

## Consequences

The API is additive; applications that use none of it keep their behaviour and emit no new timers. Each live
clock-dependent subscription holds one timer, and all subscriptions in a zone re-evaluate at the same instant
(unmeasured). Tests gain a test-only package. This record ships no API and proves no implementation.

Platform prerequisites, taken from the .NET 10 runtime sources the review cited (`TimeZoneInfo.FullGlobalizationData.cs:16-18`,
`TimeZoneInfo.cs:654-658`) and not reproduced locally:

- The default `UTC` resolves everywhere, including without tzdata and in invariant-globalization mode.
- A non-UTC IANA id needs time-zone data. On Linux and macOS that means a tzdata database, which slim or distroless
  images may lack. On Windows, .NET converts IANA ids through ICU, so hosts that use NLS globalization
  (`System.Globalization.UseNls`) or invariant mode cannot resolve them.
- The validator turns a missing prerequisite into a start-time failure that names the id.

Whether Cratis images meet these prerequisites is unverified. Implementation adds a deployment test matrix: UTC and a
non-UTC IANA id on a Linux image with tzdata, a Linux image without tzdata, a Linux image in invariant mode, Windows
with ICU and Windows with NLS. It expects success for UTC everywhere, success for the non-UTC id where data is
present, and a named start-time failure otherwise. The documentation lists the prerequisites.
