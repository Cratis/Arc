# Ada upstream backlog — Arc

**13 open items: 7 defect reports and 6 improvement proposals**, plus four cross-repo items with an Arc half. All were found while building **Ada**, a large production Cratis application — event-sourced backend on .NET 10, React/TypeScript frontend. Nothing here has been handed over upstream yet; you are the first reader.

**To work this list:** tell your agent *"work through `ADA-UPSTREAM.md`"*. Take the items in the order given, one at a time. Complete each — verify, reproduce, fix, gate, report — before starting the next. Do not batch items into one branch or one commit.

**Where the full reports live** (each is self-contained and cold-readable; read the whole file before touching code):

| | Path |
|---|---|
| Defect reports | `/Volumes/sourcecode/repos/hive/Ada/Planning/upstream/<ID>.md` |
| Defect index | `/Volumes/sourcecode/repos/hive/Ada/Planning/CRATIS_UPSTREAM_PROMPTS.md` |
| Improvement proposals | `/Volumes/sourcecode/repos/hive/Ada/Planning/upstream-improvements/<ID>.md` |
| Improvement index | `/Volumes/sourcecode/repos/hive/Ada/Planning/CRATIS_UPSTREAM_IMPROVEMENTS.md` |

Every citation in every report was taken at Arc tag **`v20.69.2`** (`6a7d03fed`). If HEAD has moved, `git diff v20.69.2 HEAD -- <cited file>` before trusting a `file:line`.

---

## How to work these

Ada is a consumer, not an authority on this codebase. Every report is a high-quality hypothesis with evidence attached — never a specification.

1. **Re-verify before anything else.** Open every cited `file:line` and confirm it still reads as reported.
2. **Trust the report's own honesty markers.** Each file has an *Honest limitations* section stating which claims were **observed live** and which were **traced by reading source**. A source-traced claim is unproven until you run it. Several reports also record claims of their own that were later withdrawn, corrected or challenged — read those, they mark where the reasoning was hardest.
3. **Reproduce inside this repo before fixing.** Failing spec first, then the fix. Mutation-prove it: red before, green after, red again with the fix deleted.
   - ⚠️ **Items 1, 2 and 5 are generator defects.** The artifact under test is emitted TypeScript, so the reproduction asserts over generated output, not C# behaviour. Nobody diffs a generated file, which is exactly why all three shipped.
   - ⚠️ **Item 4 is a browser-side request-count claim**, and the report states plainly that no request count in it was ever observed. The deciding experiment is a network trace.
4. **The "suggested fix" is a suggestion.** Arc owns the design. The *symptom* is the falsifiable part and must stop happening; the remedy is Ada's opinion, and you should reject it freely.
5. **⚠️ Standing ruling this corpus was re-scoped under on 2026-08-02: a Cratis-authored error message is a developer diagnostic, not end-user copy.** Items 3a and 3b therefore ask for a **machine-readable discriminator**, not for translatable text. If a fix starts reading as *"make this sentence localizable"*, you have the wrong end of it. Item 3b's client-side `` `${name} is required` `` instance is **withdrawn** — do not re-file it.
6. **If an item does not reproduce, that is a valid outcome.** Record what you ran and move on. If it is blocked on a ruling Arc owns, state the question and move on.
7. **Gates:** this repo's full build + spec gates, zero warnings, and cite the actual output — not a recollection of it.

**Improvement proposals (items 7–12) are read differently, and the difference matters.** A defect is a bug report — symptom, reproduction, fix. An improvement is a *design conversation*: nothing is broken, and the shape is Arc's call, not the implementer's. For those items:

8. **Start with the proposal's *"Proposal vs. established fact"* section.** It separates what was verified in this repo from what is Ada's *suggested shape*. Arc has agreed to none of the latter. An agent that skips this section confidently implements a design nobody chose.
9. **Respect *"⚠️ What is explicitly not being asked for."*** That is Ada drawing the line between a seam Cratis should own and Ada's own taste. Don't widen past it — and don't narrow the seam down to Ada's specific use either.
10. **The *Implementation brief* is the working document**: pinned commit, current behaviour at cited `file:line`, every touch point, existing specs and the specs the change needs, build/test commands, blast radius, wire-compatibility verdict, acceptance criterion, and what is still owed.
11. **Where an option is marked *needs design*, or a prerequisite is unmet — stop and put the question** rather than picking one. A proposal implemented past its open ruling is worse than one not started.

**Report per item:** which claims you **confirmed, refuted or corrected**, and the corrected mechanism — Ada wants these back, and a refuted claim is as valuable as a confirmed one. Then the reproduction and mutation evidence (or, for an improvement, the option chosen and every open design question you had to answer); anything asked for that you deliberately did not do, and why; any adjacent defect you found on the way.

Work on a branch per item. Do not push or open a PR unless asked.

---

## The list

### 1. ARC-23 — four distinct C# temporal types all map onto `Date`

**Defect · 🔴 High · the lowest-friction item here**

`DateTime`, `DateTimeOffset`, `DateOnly` **and** `TimeOnly` all emit as `@field(Date)` (`TypeExtensions.cs:61,62,65,66`). A calendar date becomes a UTC-midnight instant and renders **one day early west of UTC** — and *correct* for developers in Europe, which is what let it ship. The converter registry in `@cratis/fundamentals` is module-private, so no consumer can correct it downstream.

The off-by-one was **run, not inferred**: `new Date("2026-05-12")` → `2026-05-12T00:00:00.000Z` → renders `2026-05-11` in `America/New_York`. The user-facing symptom was observed live in Ada before the mitigation shipped.

**Lowest friction in the set:** it asks for a map entry and a converter that both follow an already-shipped pattern (`Guid`, `TimeSpan`). The Fundamentals half rides as **IMP-10** in that repo's list. What a consumer really gains is the removal of a per-call-site obligation — a distinct wire type makes the mistake unrepresentable instead of merely tested-against.

### 2. ARC-21 — the proxy generator resolves a deferred message factory at generation time and freezes the result

**Defect · 🔴 High**

`ValidationRulesExtractor` `DynamicInvoke`s a deferred `.WithMessage` factory **at generation time** and bakes the returned literal into the generated client validator — which then short-circuits the server round trip that would have resolved it correctly. **Commands *and* queries**: one shared extractor feeds four generation paths.

⭐ **Measured across a whole application**: 78 `withMessage`-carrying proxies, 84 + 3 distinct strings, and **90 of 488 localized validator messages (18%) unreachable** — silently, with no build diagnostic.

Ada's workaround is an application-wide prototype opt-out plus a meta-spec that keeps its host-entry-point import from being dropped; the price is forgoing the generated client rules wholesale, which is what makes item 4 load-bearing.

### 3a. ARC-20 — a concurrency violation is flattened into an English sentence

**Defect · Medium-High · one pair with 3b**

Chronicle's structured `ConcurrencyViolation` is interpolated into an English sentence and added as an untyped `ValidationResult`; `CommandResult` has no concurrency member. So a client can only detect a retry-able race by **matching prose**, and the internal sequence numbers reach the user untranslated.

The English string was **observed live** reaching Ada's UI. Not verified: no spec pins it, and no in-memory tier can produce a `ConcurrencyViolation` at all.

### 3b. ARC-22 — a thrown validator's result is indistinguishable from a genuine rejection

**Defect · Medium-High · one pair with 3a**

When a validator **throws**, `CouldNotValidateMessage` is returned as that validator's *entire* result set (`ValidatorInvoker.cs:26`, `:56`) — displacing the authored rejection reason with a `ValidationResult` shaped **exactly** like a genuine one (`Error`, free text, no members, `State` `null`). A consumer cannot detect the substitution except by matching English prose.

Observed live in Ada on 2026-08-02: a Norwegian-locale user received the English constant in place of a translated rule message. The only available mitigation is pinning a literal from Arc's source.

**Why 3a and 3b are one pair:** both ask for the same missing `ValidationResult.State` discriminator on a framework-composed rejection, so a fix for either is most of a fix for the other. **Item 8 (IMP-2) is the general form of both** — settling it makes these two nearly free.

### 4. ARC-24 — `autoServerValidateThrottle` governs a second, redundant request and not the per-keystroke one

**Defect · Medium**

With `autoServerValidate` on, `CommandFormFields` POSTs to `{route}/validate` **per value change, unthrottled** — a direct `await runCommandValidation(...)` in `onChange` with no timer between it and `instance.validate()`. `autoServerValidateThrottle` governs a *different* effect that fires only once all fields are valid, and `validateOn` gates display rather than requests.

**The correct configuration is the chattiest one**, and there is no consumer lever short of disabling the feature.

**This is item 2's cost.** With the generated client rules disabled to work around ARC-21, server validation is the only write-time feedback left — 59 dialog consumers plus 5 standalone forms in Ada depend on it. Fixing item 2 reduces the exposure but does not close this: rules the generator cannot express still need the server on every keystroke.

⚠️ The unthrottled per-change POST is **certain** from source. The compounding second, throttled request is **traced from the dependency array, not measured**.

### 5. ARC-26 — the proxy generator flattens XML docs with `XElement.Value`, erasing every self-closing element

**Defect · Medium-High**

XML docs are copied into the `.ts` proxy through `XElement.Value` (`XmlDocumentation.cs:140`, plus direct reads at `:81` and `:104`), which flattens to text. Every **self-closing** element — `<see cref/>`, `<seealso/>`, `<paramref/>`, `<see langword/>` — is erased and the prose fuses around the hole, leaving only a doubled space. The file contains **no element-name handling at all**, so this is flattening rather than stripping and no self-closing element can survive.

Worse the more idiomatically the C# is documented, and invisible because nobody diffs a generated artifact. The mangled output was seen in Ada's own shipped `.ts` before the workaround: *"the manual signed-PDF path —  and  decide against ContractPolicy"*.

Ada pays for it with a `<see cref>` ban in its rule corpus, an analyzer carve-out that ban forces, and a whole meta-spec that mechanizes it — a rule, an exemption and a guard, all to route around one call to `.Value`.

### 6. ARC-27 — the docs state that routing-only stream metadata does not affect concurrency control, and it does

**Defect · Documentation · 🔴 High**

*"These metadata attributes tag the appended events without affecting concurrency control"* is **false**: the value enters the command context and reaches the append regardless of the flag, and the fallback strategy resolves its expected tail *from that metadata*. Repeated at `concurrency.md:74`, incomplete at `:101`, **and written into a spec's XML rationale** — which is what makes it a misconception rather than a typo. A fifth row, `events.md:217`, misstates the same mechanism for `EventForEventSourceId`.

**This is the only report in the whole corpus whose cost can be *shown* rather than asserted:** the false sentence propagated into a consumer's rule corpus and was caught there by an independent review as `[BLOCKING]`.

⚠️ **It has already survived a refutation attempt, and that matters for how you read it.** A third independent reader traced the same code, reached the documentation's conclusion, and called the report refuted — stopping one link short at Arc's `return null`, which `BuildFor`'s own `<returns>` describes as handing off to the event sequence's configured strategy, and `EventLog` **is** an `EventSequence`. The challenge is recorded inside the report and was then refuted **by experiment**: 25 `[KernelFact]`s, real kernel + real Mongo, mutation-proven, 8 consecutive identical runs.

**If you find yourself re-deriving the challenge, you are re-deriving the misconception the report is about** — read its recorded rebuttal before concluding. ⚠️ Arc's own half (the three value providers → the command context → the append) is still **not measured**; the substitution the report turns on is.

Asks for no behaviour change at all: five prose sentences and one spec's stated rationale. **Pairs with Chronicle's `CHR-43`** — the same attributes documented wrongly on the observer side. Neither asks for a behaviour change; both ask the documentation to move to the code.

---

## Improvement proposals

From here down, nothing is broken. Read discipline points 8–11 above before starting any of these.

### 7. IMP-18 — which serialization boundary a command-side read model crosses is decided by provider ownership, and nothing says so

**Improvement · ✅ Executable — the cheapest change in the whole register**

**One `<remarks>` paragraph**, appended to prose that is already there and already correct. No wire, no behaviour, no spec churn. It would have prevented Ada from shipping a fix whose reach it had misjudged.

The observability option — a per-type resolution accessor — is a public-surface call and a separate decision.

### 8. IMP-8 — a raw query parameter silently bypasses the `ConceptValidator<T>` Arc would otherwise have run

**Improvement · ✅ Executable — premise now verified**

A raw `string`/`Guid` query parameter converted to a concept in the body silently bypasses the `ConceptValidator<T>` Arc would have run; `ARC0001` inspects only the return shape. **1 new analyzer + descriptor + `AnalyzerReleases` line + specs; next free id is `ARC0015`.**

⭐ The defect it was built from **survived three refactors** of one query. The analyzer names only Cratis types and is liftable as-is.

### 9. IMP-1 — `Cratis.Arc.Testing` has no seam for strengthening a spec assertion

**Improvement · ✅ Executable — hook *shape* is the open question · ranked #1 on evidence**

`ShouldHaveValidationErrors` is a plain static with no extension point, so a repo-wide spec policy can only be installed by **namespace shadowing** — invisible at the call site, silently disarmed by a file move, and payable once per root namespace.

⭐ Ada paid it **twice**, once per host; the second payment needed an `#if` plus a linked-source construction. 46 of 489 specs measured affected.

**Size: 1 file (9 call sites) plus a net-new spec project** — `Cratis.Arc.Testing` has none today.

### 10. IMP-2 — a rejection carries no machine-readable identity

**Improvement · ⚠️ Options 1–2 executable · option 3 needs design · ranked #2**

A `ValidationResult` has no rule id or code; `State` is populated and **never read back**; FluentValidation's `ErrorCode` is dropped. So a domain rule, a constraint violation, a concurrency violation and a pipeline failure all arrive as `ValidationResult(Error, <free text>)` and nothing downstream can tell them apart.

⭐ 46/489 specs; a 194-line workaround compiled into two hosts; **Arc's own analyzer already splits *Rejections* from *NamedRejections***.

**Size: 14 C# producers + 4 restatements of the shape in TypeScript + testing + Screenplay + docs.** ⚠️ **Do not start before ruling on the provenance categories** — that ruling is the task.

**This is the general form of items 3a and 3b.** Settle it here and both get much cheaper.

### 11. IMP-3 — a validator dependency the pipeline cannot resolve is reported as a business-rule rejection

**Improvement · ✅ Option 1 executable · blocked on item 10**

An unresolvable **read-model** validator dependency is rejected with *"The command targets an entity that does not exist."* in `ValidationResults` — before any rule runs, and indistinguishable from a rule rejection.

⭐ **Mutation-proven**: neutering one rule left **all 29** of a slice's specs passing.

**Size: 2 lines + specs. ⚠️ Do not start before item 10 — this is its decision applied twice.**

### 12. IMP-14 — a command form field is identified only by a mutable `displayName` string

**Improvement · ✅ Options 1 + 3 executable · option 2 is a public-surface call**

A `CommandForm` child is a field **only** if `component.displayName === 'CommandFormField'` — **6 read sites and 4 stamps here**, plus 1 column read and 2 column stamps in the **Components** repo (a seventh site compares `'CommandFormColumn'`, the same shape). Any transform that sets `displayName` unbinds every field with no error and no warning.

⭐ **Measured, not traced**: the same source module observed under two pipelines with two different `displayName` values, and the unbinding confirmed in the DOM.
⚠️ **Honest limitation: Storybook-only — no shipped dialog was ever affected.** This is a robustness proposal, not an outage report.

⚠️⚠️ **Arc and Components must ship together.** A version skew between them **reproduces the exact failure the change fixes**. Coordinate the release, or don't make the change. Choosing the marker shape is the open question and is Cratis's call.

⚠️ A counting correction is already on the record: **six** field read sites, not seven, and one earlier *"no upstream spec"* claim was wrong — exactly one Components spec stamps `'CommandFormField'` while three siblings do not. Recount before quoting any number.

---

## Cross-repo notes

| Item | Also touches | Driven from |
|---|---|---|
| 1 (ARC-23) | A converter-registration seam — rides as `IMP-10` | Fundamentals |
| 6 (ARC-27) | `CHR-43` — the same attributes documented wrongly on the observer side | Chronicle |
| 12 (IMP-14) | The column half — **must ship together** | Components |
| — | `IMP-11`, `IMP-13`, `IMP-16` each have an Arc-side half | Chronicle |

`ARC-25` carries an `ARC-` id because the template's frontend scaffold is Arc's (`@cratis/arc.vite`) — but it is **Templates** repo work and is not in this list.
