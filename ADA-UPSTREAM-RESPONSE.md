# Response to Ada's upstream backlog

All 13 open items worked, on branch `ada-upstream-backlog`. This is the per-item report [`ADA-UPSTREAM.md`](ADA-UPSTREAM.md) asks for: which claims were **confirmed, refuted or corrected**, the reproduction and mutation evidence, the options chosen and the open questions answered, anything deliberately not done, and the adjacent defects found on the way.

**Headline: no report was refuted.** Every one was confirmed on its central claim. Five carried something wrong or understated in the detail, and those corrections are the part worth reading — they are collected in [Corrections to the reports](#corrections-to-the-reports).

**Gates.** Debug and Release build clean. 7,377 C# specs across 15 projects, 0 failures. 1,191 TypeScript tests across 5 workspaces. `yarn lint` and `npx tsc -b` clean on every workspace. Pre-existing and unrelated: one `CS0436` warning in `TestApps/Chronicle` (reproduced on a clean tree with `--no-incremental`), and `Documentation/verify-links.sh` reporting 15 broken links — all external URLs returning status `0`, reproduced identically on a clean tree.

---

## Corrections to the reports

The five places a report was wrong or understated. Each is checkable against the specs named.

### 1. ARC-23 understates `TimeOnly`, and the correction makes it more severe

The report says a `TimeOnly` "becomes a `Date` pinned to 1970-01-01" and that "the same local-getter rendering shifts it across a day boundary."

It does not. `new Date("14:30:45")` is **`Invalid Date`**. The value is destroyed outright rather than shifted — there is no instant, no rendering, nothing to shift. `TimeOnly` is total data loss, not a timezone bug.

Observed, not traced: `when_deserializing_temporal_properties_through_the_generated_proxy`, which runs the real generated proxy through the real `JsonSerializer` in V8.

### 2. ARC-24's unmeasured claim is true, and is now measured

The report states plainly that no request count in it was ever observed, and marks the compounding second request as "traced from the dependency array, not measured."

Measured. Ten characters typed into one field, counting POSTs to `{route}/validate`:

| | requests |
|---|---|
| default throttle (500 ms) | **11** — ten per-character, plus one trailing |
| throttle raised to 2000 ms | **10** — the prop moved nothing |
| after the fix | **1** and **0** |

The eleventh request is the compounding one the report could not confirm. It appears only at the default throttle, because at 2000 ms it had not fired within the settle window — which is also what proves the two paths are distinct.

`when_typing_into_a_field_with_autoServerValidate`.

### 3. ARC-24 is a documentation divergence, not a design surprise

The report frames this as a prop that does not do what consumers assume. It is stronger than that: Arc's own shipped documentation already promises the fixed behaviour.

`Documentation/frontend/react/command-form/auto-server-validation.md`, unchanged, on typing a 17-character value:

> - Without throttle: 17 server calls (one per character)
> - **With throttle: 1 server call (after user stops typing for 500ms)**

and, on the same page, *"`autoServerValidateThrottle` delays server validation, not client validation"* and *"Server validation only triggers when ALL client validations pass"* — both exactly what the code now does. **The documentation needed no change.** The code had diverged from it.

### 4. ARC-27's last unmeasured link is now measured

The report's *Honest limitations* records that Arc's own half — that a routing-only `[Command]` reaches `Append` with its tag intact — "remains a trace and not an observation", and names the deciding experiment as still owed.

Owed no longer. `for_SingleEventCommandResponseValueHandler/when_handling_with_metadata/without_concurrency_on_event_source_type` pins both halves of the pair: the handler passes **no concurrency scope** and **the routing tag regardless**. Mutation-proven by replacing `commandContext.GetEventSourceType()` with `null` in the handler, which reds it.

With Ada's two kernel-tier experiments covering the Chronicle side, the chain is now measured end to end.

### 5. ARC-21's frozen literal was specified, not accidental

The report does not say whether anything pinned the behaviour. Two specs did, and one was named for it:

- `for_ValidationRulesExtractor/…/should_carry_the_lazily_resolved_message`
- `for_ProxyGeneration/…/should_resolve_the_lazily_declared_message`

Both inverted, each carrying the reason. That the eager resolution was written down as intended is why the fix is a change of mind rather than a bug fix, and it belongs in the report.

---

## Per item

### 1 · ARC-23 — four temporal types collapse onto `Date`

**Confirmed.** Every citation exact at HEAD; `git diff v20.69.2 HEAD` on `TypeExtensions.cs` is empty. The off-by-one reproduced inside Arc: `"2026-05-12"` through the generated proxy renders `2026-05-11` in `America/New_York`.

**Confirmed and strengthened — no consumer-side fix exists.** `@cratis/fundamentals` ships no `DateOnly` or `TimeOnly` type at all, in the installed package *or* in the Fundamentals repo source. And an Arc-defined class would not work either: with the converter registry module-private, `deserializeValueFromField` falls through to `JsonSerializer.deserialize(type, …)` and returns an **empty instance**.

**Remedy rejected.** The suggested `DateOnly` class in `@cratis/fundamentals` is not Arc's to ship, and emitting `@field(DateOnly)` today would generate TypeScript that does not compile. Mapped both to `string` instead: their JSON wire form already *is* the ISO string, so nothing is invented and nothing lost, and the mistake becomes unrepresentable — you cannot render a string with an instant renderer. When Fundamentals ships the type this is a two-line change, and `string` is strictly closer to that end state than `Date`.

**Deliberately not done.** The `DateTime`/`DateTimeOffset` collapse (secondary point 1). Both are instants, `Date` is right for both, and distinguishing them needs a wire type that does not exist.

**Evidence.** Red 5 / green 9 / mutation 5. Two tiers: over the emitted TypeScript, and through the generated proxy in V8.

**Semver: major.** `framework.md` treats generated-proxy shape as public API, and the TypeScript type of every `DateOnly` property changes.

### 2 · ARC-21 — deferred message factory resolved at generation time

**Confirmed**, including that one extractor feeds all four generation paths — so command and query specs were both written, and a command-only fix would have looked complete.

**Fix.** Removed the `_errorMessageFactory` branch. A delegate is opaque; there is no way to tell a factory returning a constant from one reading the culture, so not calling it is the only guess-free move. The eager `_errorMessage` branch is untouched — a literal genuinely is context-free.

**Honest about what this does not do.** It stops the generator freezing a context-dependent value. It does **not** make a mirrored rule's message localized: the client rule falls back to its own default, which is also English. The authored message only becomes reachable if the client stops short-circuiting, which the report explicitly forbids touching. Ada should know the localization symptom is not closed by this.

**Evidence.** Red 5 / green 13 / mutation 5, at three tiers (extractor, generated command, generated query).

### 3a · ARC-20 — concurrency violation flattened into prose

**Confirmed**, all citations exact.

**Ruling made — this is IMP-2's, arrived at here.** Added `ValidationResultReason`, an open `ConceptAs<string>` with a defaulted init-only `Reason` on `ValidationResult`.

- **Not `State`.** That slot already carries FluentValidation's `CustomState` (`ValidatorInvoker.cs:47`), so it belongs to whoever wrote the rule. The report proposed it; it is the wrong slot.
- **Not an enum.** Rejections are composed in Arc, in Chronicle *and* in consumer code; a closed set makes every new kind a breaking change for anyone switching over it. This also sidesteps IMP-2's open question 2, which flags that enum-vs-string JSON serialization was not establishable.

Applied to all three concurrency sites and to the constraint path, which was equally mislabeled as an authored rule.

**Adjacent gap found.** The aggregate-root path had **no concurrency spec at all** — which is how it came to carry the same flattening as the event-log path unnoticed. Added.

**Evidence.** Red 5 / green / mutation 5 in C#; the TypeScript rehydration mutation-proven separately. A round-trip spec pins that the reason serializes as a plain string and reaches the browser.

**Semver: minor** on the wire (additive), but **source-breaking for object-literal construction** of the TypeScript `ValidationResult` — caught by `tsc -b`, fixed in two stories. `reason` is required on the class (readers get a guaranteed value) and optional on the wire type (a server that predates it reads as `rule`).

### 3b · ARC-22 — thrown validator indistinguishable from a rejection

**Confirmed.** One line: the substituted result now carries `ValidatorFailed`.

Fail-closed behaviour, the message text, and the server-side logging are all unchanged, deliberately — the report is right that making the constant translatable would invite displaying it.

**Adjacent gap found.** `ValidatorInvoker` had **no direct spec coverage of any kind**. Added, including the contrast case proving an authored rejection still reads as `Rule` and keeps its members and its author's state.

**Evidence.** Red 3 / green 20 / mutation 3.

### 4 · ARC-24 — the throttle governs the wrong request

**Confirmed and measured** — see [correction 2](#2-arc-24s-unmeasured-claim-is-true-and-is-now-measured) and [correction 3](#3-arc-24-is-a-documentation-divergence-not-a-design-surprise).

**Option chosen: collapse to one path** (the report's design point 2) rather than debounce both. The per-change validation stays immediate but goes client-side only — still the immediate driver of `isValid`, at no latency and no network — and the existing throttled effect becomes the single server path. That effect now also feeds `silentValidationResult`, so a rule only the server can express still has the final say on `isValid`, once typing stops rather than once per character.

**Also tightened** the existing throttle spec, whose `toBeLessThanOrEqual(5)` is what let a round trip per keystroke hide underneath it.

**Deliberately not done.** The blur-path request. One per blur is a discrete user action and reasonable; it is not the reported symptom.

### 5 · ARC-26 — XML docs flattened with `XElement.Value`

**Confirmed**, and the reproduction produced the report's own example string byte-for-byte: `A  and a gadget.` All three code paths.

**Fix.** One element-aware walk shared by all three entry points: a `cref` renders as `{@link Name}`, a `langword` and a `paramref` as inline code, `<c>` keeps its backticks, an explicit label wins, anything else keeps its prose, and whitespace collapses last so nothing rendered away leaves a seam.

**Adjacent defect found — not in the report.** The property-summary path only `.Trim()`ed and never rejoined lines, so a multi-line `<summary>` spilled its source newlines and indentation straight into the single-line JSDoc. Routing all three through one renderer fixes it. **Visible in the diff**: the Release build regenerated four checked-in TestApp proxies, and the change is right there —

```diff
-     * Gets the fully qualified query name (e.g. MyApp.Authors.Listing.AllAuthors).
-            Matches the queryName property on the generated TypeScript proxy.
+     * Gets the fully qualified query name (e.g. `MyApp.Authors.Listing.AllAuthors`). Matches the `queryName` property on the generated TypeScript proxy.
```

**Evidence.** Red 7 / green 22 / mutation 7, plus the regenerated proxies.

### 6 · ARC-27 — the docs contradict the code on routing-only metadata

**Confirmed.** All five citations verbatim at HEAD. I did **not** re-derive the challenge — see [correction 4](#4-arc-27s-last-unmeasured-link-is-now-measured) for what I added instead.

**Five edits made**, including the one the report calls the most important: the `ConcurrencyScopeBuilder` spec's rationale, which stated the same false inference in source. Its assertions were always right; only the reason given for them was wrong, which is what makes this a misconception rather than a stale page.

Also added a *What a routing-only tag already does* section carrying all three consequences — including the counter-intuitive third, that declaring `concurrency: true` on a **subset** produces a strictly *broader* scope than declaring it on none, which no page mentioned.

**No behaviour change**, exactly as asked. Making a routing-only tag concurrency-inert would silently widen every existing consumer's guard.

### 7 · IMP-18 — which serialization boundary provider ownership decides

**Confirmed.** All established facts re-verified.

**Option 1 + 2 taken** (the ownership `<remarks>`, and each resolver's own). Option 3, the resolution accessor, deliberately not done — the proposal itself calls it a separate public-surface decision.

**"Still owed" discharged.** The proposal records EF Core's materialization path as unknown and says the docs paragraph should not name it until someone establishes it. It is `DbContext.FindAsync` through EF Core's own entity model — read here, so the paragraph names all three providers rather than two.

### 8 · IMP-8 — raw query parameter bypasses `ConceptValidator<T>`

**Confirmed.** New analyzer `ARC0015`, descriptor, `AnalyzerReleases.Unshipped.md` line, 7 specs.

**Three open questions answered:**

| Question | Ruling | Why |
|---|---|---|
| Public-only or public-and-internal? | **Both** | Query discovery registers internal methods — `FindGenericQueryShapedMethods`' own `<remarks>` says so. Public-only would under-reach on a real query. |
| Severity? | **Warning** | The shape is legal and retyping is not behaviour-neutral. A rule that breaks a warnings-as-errors build on legal code gets suppressed, not adopted. Both carve-outs are in the descriptor's `description`. |
| `EventSourceId` arm? | **Dropped** | It *is* a `ConceptAs<string>` and is matched anyway, which keeps a Chronicle namespace out of a package that does not know Chronicle exists. |

**Correction to the brief.** The nullable pin it asks for is unreachable as written: there is no conversion from `Guid?` to a concept, so the author writes `(RequestId)id.Value` and the conversion sits over the property access, not the parameter. A rule matching only the bare reference is silent on exactly the shape a nullable parameter forces. The analyzer sees through the `.Value`.

**Flagged for the reviewer, as the brief asks:** the operation-block registration has no precedent in either analyzer project here.

### 9 · IMP-1 — no seam for strengthening a spec assertion

**Confirmed**, including that `Cratis.Arc.Testing` had no spec project and its assertions had zero direct coverage. Both added.

**Three open questions answered:**

| Question | Ruling | Why |
|---|---|---|
| Process-global or per-scenario? | **Discovered, not a settable static** | Per-scenario is unreachable — the assertions extend `CommandResult`, not the scenario. A mutable static would be shared state across a parallel xUnit run. Discovery is process-wide but immutable, and is the pattern `ICommandScenarioExtender` already uses. |
| Before or after; can it veto a pass? | **After, strengthen only** | A passing assertion may become a failure; a failing one may never become a pass. The package's own guarantees are not negotiable by a consumer. |
| Exception carries the `CommandResult`? | **No** | Not needed by the use, and a public-API addition for nothing. |

**Scope widened past the proposal's "bonus, not a requirement":** all nine assertions, not just `ShouldHaveValidationErrors`. A seam covering one assertion and not its siblings is a strange public surface, and `CallerMemberName` means adding an assertion needs no second edit and mints no vocabulary parallel to the names Screenplay already matches.

**Honest note.** The specs are serialized into one xUnit collection. Policies are discovered once per process, so the seam really is process-wide; pretending otherwise in the specs would hide that from a reader.

### 10 · IMP-2 — a rejection carries no machine-readable identity

**The ruling this item exists for was made in 3a**; this completed it across every producer.

Two further categories: `DependencyUnavailable` (a read model the pipeline could not resolve) and `MalformedRequest` (a body that could not be read, or a value that could not be bound). An authored rule stays `Rule` — a DataAnnotation attribute and an aggregate's own `Failed()` are the author's rules, not the framework's.

Added `ShouldHaveValidationErrorBecauseOf` and listed it in Screenplay's `NamedRejections`, where matching is by name string.

**Option 2 rejected.** Carrying FluentValidation's `ErrorCode` is one line, but it would make that library's defaults — `NotEmptyValidator`, `NotNullValidator` — a wire-visible value consumers depend on and Arc then owes. `WithState` remains the seam for a rule that wants to name itself.

### 11 · IMP-3 — unresolvable dependency reported as a rule rejection

**Option 1 landed with item 10.** Both sites now carry `DependencyUnavailable`.

**Option 2 unblocked and taken.** The proposal withdrew it as impossible without option 1, because from a `CommandResult` the only signal was a literal string that a genuine absent-entity rejection also produces. With the reason present it needs no string matching: `ShouldHaveValidationErrors` now refuses to pass when a dependency failure is the **whole** story, naming `ShouldHaveValidationErrorBecauseOf` as the way to assert the case deliberately.

Only when it is the whole story — a result carrying a real rule rejection alongside has a rule that ran, so the assertion has something to be about and stands.

⚠️ **This is a deliberate behaviour change to the testing package.** A spec passing only on a dependency failure will now fail. That is the point: it was asserting nothing. Ada's 29-spec measurement is exactly the shape it reds.

### 12 · IMP-14 — field identified by a mutable `displayName`

**Confirmed.** Recount, as instructed: Arc has **3 field read sites, 1 column read, 4 field stamps, 1 column stamp**. The corrected "six field read sites" is the total across both repositories — the other three are Components'.

**Options 1 + 3 taken; option 2 deliberately not.** A static marker checked first with the `displayName` comparison kept as fallback, plus the doc caution. The `Symbol` resists a name-based transform more thoroughly but is a public-surface choice for Cratis, and it needs `Symbol.for` to survive a duplicate install — two reasons to put the question rather than answer it.

**On the shipping constraint.** The backlog warns that skew "reproduces the exact failure the change fixes". That hazard is specifically about *replacing* the string check — the report's own blast-radius section says options 1 and 2 are "purely additive as long as the `displayName` check stays", and that the fallback "is what makes the two versions interoperate in both directions". Keeping it makes the Arc half safe to ship alone.

⚠️ **Still owed, and not Arc's to do:** the three read sites in `@cratis/components`. Until they move, a field surviving a `displayName` rewrite is recognized by `CommandForm` and not by `CommandDialog`. Nothing regresses meanwhile.

---

## Adjacent defects and gaps found

| | Where | Status |
|---|---|---|
| Multi-line property `<summary>` spilled raw newlines into the generated JSDoc | `XmlDocumentation` property path | Fixed with item 5 |
| Aggregate-root path had no concurrency coverage at all | `for_AggregateRootCommitResultExtensions` | Added with item 3a |
| `ValidatorInvoker` had no direct spec coverage at all | `Arc.Core.Specs` | Added with item 3b |
| `Cratis.Arc.Testing` had no spec project and no assertion coverage | net-new `Testing.Specs` | Added with item 9 |
| A throttle spec's `toBeLessThanOrEqual(5)` hid a per-keystroke round trip | `when_autoServerValidate_with_throttle` | Tightened to `toBe(1)` with item 4 |
| Corrupt NuGet cache entry blocked restore (`system.text.encodings.web/10.0.10` extracted without its `.nupkg`) | environment | Cleared |

## Deliberately not done

- **ARC-23:** the `DateTime`/`DateTimeOffset` split — both are instants.
- **ARC-24:** the blur-path request — one per blur is a discrete user action.
- **ARC-22:** the withdrawn client-side `` `${name} is required` `` instance — left alone as instructed.
- **IMP-18:** the resolution accessor — a separate public-surface decision.
- **IMP-2:** carrying `ErrorCode` — would make FluentValidation's defaults Arc's contract.
- **IMP-1:** putting the `CommandResult` on the assertion exception.
- **IMP-14:** the `Symbol` marker, and the Components half.

## Open questions for Cratis

1. **Is `string` the right wire type for `DateOnly`/`TimeOnly`, or should Fundamentals ship the types?** The `string` mapping is correct and unblocked today; a first-class type is better and needs a Fundamentals release. Note another session is currently building a generator type-mapping extension point on `feature/proxy-generator-type-mappings`, which is IMP-10's generator half — these two should be reconciled.
2. **Should `ValidationResultReason` be closed?** It is open on purpose so Chronicle and consumers can mint values. If Cratis would rather own the vocabulary, an enum plus a serialization ruling is the alternative.
3. **Is the `ShouldHaveValidationErrors` behaviour change acceptable?** It reds specs that were passing on nothing. Correct, and still a break.
4. **`Symbol` or static property for the command-form marker**, and when do Arc and Components ship together?
