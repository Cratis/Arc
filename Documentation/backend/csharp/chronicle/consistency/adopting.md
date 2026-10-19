---
title: Adopting read protection
description: Move an existing application from Off to On through Measure, read the Measure report, fix what it shows, and lower the mode at run time when you must.
---

Use this page when you have commands that work today and want read protection without breaking them. New code can start on [Getting started](./getting-started.md) with `On`.

## The path

| Step | Mode | What you do | Behaviour change |
| --- | --- | --- | --- |
| 1 | `Off` | Update the packages. Nothing else | None |
| 2 | `Measure` | Set `<CratisReadProtection>Measure</CratisReadProtection>`. Run the application and your tests | None for callers. Arc records what `On` would do |
| 3 | `Measure` | Read the report. Fix each refused read, or annotate it `[Unprotected("why")]` | The fixes are ordinary code changes |
| 4 | `On`, per project | Switch one project at a time. Run its tests | Reads are guarded. Refused reads fail the command |

You can mix modes. A command-level `[ReadProtection(ReadProtectionMode.Off)]` keeps one command off while its project is `On`, and `[ReadProtection(ReadProtectionMode.On)]` turns one command on in an `Off` project. A command-level `[Unprotected]` means `Off` for that command.

## What Measure does

Under `Measure`, reads, lookups, scopes, results and status codes stay as they are. Arc looks at each command and records what `On` would do. It never changes the outcome, and a failure inside the measurement never fails the command.

| What | When | Cost |
| --- | --- | --- |
| Classifies each read-model parameter and each other read | On the request path, locally | None beyond reflection |
| Reads the event log's tail once at the start of the command | On the request path | One awaited call, with a timeout |
| Scans for events that appeared during the command and match what it read | In the background, on a sample of commands | Background reads on the shared connection |

Measure is best-effort. A command whose tail read timed out, or whose scan was dropped, is reported as `measurement_unavailable` and counts as no evidence either way. The report is a set of hypotheses, not an enforced guard.

Measure settings are runtime configuration, in the same `ReadProtection` section as the [run-time cap](#cap-the-mode-at-run-time):

| Key | Default | Meaning |
| --- | --- | --- |
| `MeasureSampleRate` | `0.1` | Fraction of commands whose reads are scanned in the background. Between 0 and 1 |
| `MeasureTailTimeout` | `250` | Milliseconds to wait for the tail read at the start of a command. Between 1 and 5000 |
| `MeasureScanTimeout` | `5000` | Milliseconds a background scan may take. Between 1 and 5000 |
| `MeasureScanLimit` | `500` | The most events one background scan looks at. Between 1 and 500 |

## Reading the report

Measure reports through three channels.

| Channel | What you get |
| --- | --- |
| Metric | Counter `cratis.arc.read_protection.events` on the meter `Cratis.Arc.ReadProtection`, with the tags `command`, `category`, `model` and `reason` |
| Trace | A span event named `read-protection.plan` on the current activity, with the same tags |
| Log | A Warning once per command and category for `refused_read`, `immediate_append` and `scope_refusal`, and a periodic Information summary |

Start with the counter, grouped by `category` and `command`.

| Category | What it means | What to do |
| --- | --- | --- |
| `protected_read` | The read would be guarded | Nothing |
| `refused_read` | The read would be refused. `reason` names the cause or the API | See the next table |
| `advisory_read` | The read is declared `[Unprotected]`. `reason` is your text | Check the reason still holds |
| `unknown_dependency` | A service or other dependency Arc cannot see | Nothing, but it is outside the guard. It is never counted as protected |
| `redirect` | The `On` lookup rule would select a different key than the one the legacy path used | Add `[LookUpBy(nameof(Property))]` to say which key you mean |
| `immediate_append` | The command appended through an `IEventLog` and `On` would refuse it | Return the event from `Handle()`, or use `IEventLog.Transactional` |
| `displacement` | An explicit scope or stream decision sits beside a read of the same source | The two must be reconciled under `On` |
| `scope_refusal` | `On` would refuse the command: its explicit scope is narrower than the read, captured after it, or incomplete | Widen the scope, or remove it |
| `coarsened` | Reads of the same source would drop stream dimensions; `reason=stream_dims_dropped` | A whole-source read beside a stream read, or reads of different streams, chooses a whole-source guard |
| `nested_concurrent`, `nested_early_completion` | A nested command ran in parallel with a sibling, or tried to commit the outer unit | Run nested commands one after another and commit in the outer command |
| `self_write` | One of the command's own events matches what it read | Informational. Own events are excluded from `matching_events_since_start` |
| `matching_events_since_start` | An event written by someone else matched the read between its start and the scan. `On` might have returned `400` | A real race. This is the data that shows where protection matters |
| `conflict_today` | An append of this command already failed | Informational |
| `measurement_unavailable` | A measurement could not be taken. `reason` says why | Raise the timeouts, or ignore it |
| `mode_lowered` | The run-time cap lowered the declared mode | See [Cap the mode at run time](#cap-the-mode-at-run-time) |

Measure does not currently emit `routing_replaced` or `routing_kept_separately`: its observed scopes do not retain the distinction between explicit guards, default routing and declared routing, including scopes derived inside Chronicle at commit. It also cannot report declared-routing coarsening from those observations. It does not emit `nested_overlap`: append notifications arrive after commit, not when an outer command stages an event before a nested read. Absence of these categories is not evidence that the cases are safe under `On`.

### Fixing refused reads

The `reason` of a `refused_read` is the Chronicle refusal. [Refusals](./refusals.md) has the full table. The common ones:

| `reason` or API | Fix |
| --- | --- |
| `Reducer` | Read a projection of the same events, or use an unfiltered reducer without constructor dependencies |
| `ReducerDependencies`, `ReducerFilters` | Remove the reducer's constructor dependencies or its event filter |
| `Join`, `Hierarchy` | Read the parts as separate models |
| `FromEventProperty`, `NotEventSourceKeyed` | Append a fact to the entity the model is keyed by, or use a constraint |
| `GenerationDeliveryNotSupported` | Use Compatibility delivery for the projection |
| `KeyConversion` | Give the model a string or `Guid` `Id` |
| `StreamKeyRequiresStreamScope` | Read the stream-keyed model with `IDecisionReads.Get` and an explicit stream in `Provide()` |
| `IReadModels`, `IEventStore`, `IEventSequence` | Take the model as a parameter, or mark the method `[Unprotected("why")]` |
| `IMongoCollection` | Inject the read model instead of its collection |
| `DetachedReadWhileReadsAreProtected` | Use `IDecisionReads.Get` in `Provide()`. A detached read is refused |

Build-time diagnostics mark many of the same sites: [ARCCHR0024](../code-analysis/ARCCHR0024.md), [ARCCHR0026](../code-analysis/ARCCHR0026.md), [ARCCHR0028](../code-analysis/ARCCHR0028.md). Raise them to errors in the projects you move.

### How noisy On is

`On` is quiet for commands over source-keyed projections. It is noisier for:

| Situation | Protected option | Honest opt-out | Roll out |
| --- | --- | --- | --- |
| Reducer-backed model | An unfiltered reducer without constructor dependencies is admitted. Otherwise re-express it as a projection | `[Unprotected("reference data; staleness tolerated")]` on the model type | Keep the project in `Measure` until it is done |
| Joined or enriched model | Separate source-keyed models with `[LookUpBy]` | `[Unprotected("copied as of read; may be stale")]` on the parameter | Keep the command `Off` |
| List or search | A constraint | `[Unprotected]` on `Provide()` | |
| Service that reads Chronicle | A parameter, or `IDecisionReads` in `Provide()` | `[Unprotected]` on `Handle()` | |

## Switching to On

Switch one project at a time.

1. Set `<CratisReadProtection>On</CratisReadProtection>` for the project.
2. Build. Fix [ARCCHR0024](../code-analysis/ARCCHR0024.md), [ARCCHR0028](../code-analysis/ARCCHR0028.md) and [ARCCHR0033](../code-analysis/ARCCHR0033.md) warnings. [ARCCHR0011](../code-analysis/ARCCHR0011.md) no longer applies under `On`.
3. Run the project's tests. Command scenarios automatically select the in-process log from the command's read-protection mode and dependencies; see [Test the race](./scenarios/testing.md).
4. Exercise every command once. Refusals happen when a command runs, not at startup.

Under `On`, discoverable validators with constructor dependencies must be registered by convention, which makes them transient. A validator registered by hand as scoped, singleton, a factory or an instance is refused. See [Validators](./validators.md).

### Upgrade note: missing protected models

Under `On`, a missing required protected read-model parameter now returns the ordinary validation reason `rule` (`ValidationResultReason.Rule`) instead of `dependencyUnavailable`, with the message `{ReadModel} '{key}' does not exist.`. HTTP status remains `400`. Update clients or specs that distinguish these reasons. Invalid protected keys produce the same rejection before a read is acquired, including for nullable parameters; nullable parameters still receive `null` for a valid missing key. `Off`, `Measure` and advisory reads keep their previous behavior. See [Missing models and invalid keys](./reads.md#missing-models-and-invalid-keys).

## Cap the mode at run time

Operations can lower the mode without a build. The cap only lowers. It never raises a mode that was not compiled in.

```json
{
  "ReadProtection": {
    "MaximumMode": "Measure"
  }
}
```

| `MaximumMode` | Effect |
| --- | --- |
| `On` (or missing, or invalid) | Declared modes apply |
| `Measure` | Commands declared `On` run as `Measure` |
| `Off` | Commands declared `On` or `Measure` run as `Off` |

At startup, Arc logs a Warning for each command whose declared mode is lowered. Measure reports a `mode_lowered` observation with the declared and effective modes. Use the cap to back out of an incident while you fix the code, and remove it afterwards: while it lowers the mode, reads run with the lower mode's behavior instead of `On`'s guards.

## Where to go next

- [Reads](./reads.md): what each read becomes under `On`.
- [Refusals](./refusals.md): every error with its fix.
- [Pitfalls](./pitfalls.md): what still fails silently.
