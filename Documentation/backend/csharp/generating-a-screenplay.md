---
title: Generating a Screenplay
description: How Arc reads the source of your application and writes the event model it already describes — what a .play file is, what it buys you, and exactly what it does and does not say.
---

An [event model](/event-modeling/) is the picture of your system: which commands change state, which events they produce, which read models those events build, and which reactors turn one thing into another. Teams draw it on a whiteboard at the start, and then the code moves on without it. Six months later the picture is fiction and nobody trusts it enough to open.

**Screenplay** is a small language for writing that picture down as text — a `.play` file — so it can live in the repository next to the code it describes. Arc can _generate_ one from your application's source. The model stops being something you maintain by hand and becomes something you regenerate, the same way the [TypeScript proxies](/arc/understanding-the-proxy-boundary/) are regenerated rather than hand-written.

## Generated from source, not from a running system

The generator reads a Roslyn compilation of your C# — the same thing the compiler sees. It never starts your application, connects to Chronicle, or looks at a deployed instance.

That choice is what makes the output useful:

- **It works from a checkout, not a live store.** You still need the CLI, a compatible .NET/MSBuild environment, restored project dependencies, and the build inputs required to obtain a meaningful compilation. No running database or Chronicle connection is needed.
- **It is diffable in a pull request.** A commit that adds a command shows up as a few lines added to the `.play`. Reviewers see the model change next to the code change, and "did this alter the event model?" becomes a question the diff answers.
- **It is reproducible.** The same source always produces byte-identical output. Everything is ordered explicitly rather than by whatever order symbols happened to arrive in, so regenerating in CI and failing on a diff is a viable check.

A runtime-derived model would answer a different question — what one deployed instance looks like right now — and would vary with configuration and environment. There is one model, and its source of truth is your source code.

```mermaid
flowchart LR
    CS["C# source — commands, events,<br/>read models, reactors"] -->|Roslyn compilation| AN["analysis"]
    AN --> M["application model"]
    M --> PLAY[".play document"]
    AN -.->|"anything it cannot express"| D["diagnostics"]
```

## Running it

The generator is used through the Cratis CLI, which ships separately from Arc:

```shell
cratis screenplay generate ./MyApp/MyApp.csproj --file MyApp.play
```

Point it at a project or a solution. A solution is one application rather than several, so its projects are read together into a single document — see [an application written as several projects](#an-application-written-as-several-projects). Anything the generator could not express is reported on the way out, and a run that reports an error exits non-zero — so a document nothing trustworthy went into never quietly looks fine. What the CLI has to do before it hands the source over is covered in [what the generator expects of its host](#what-the-generator-expects-of-its-host).

Because output is reproducible, a CI step can regenerate and fail when the committed `.play` no longer matches the source.

## Nothing is dropped silently

The gap between "what C# can say" and "what Screenplay can say" is real, and the worst thing a generator can do is paper over it. Every construct that cannot be expressed is reported as a located diagnostic — a stable code, a message you can act on, and where it came from — rather than quietly disappearing.

```text
Warning SP0019: The query 'Raw' returns 'IActionResult', which says how the result is
transported rather than what it is, so the query was left out (Library.Messaging.Feed)
```

Diagnostics come in three severities. **Information** means something is worth knowing but the document is complete. **Warning** means something was left out. **Error** means the document should not be trusted at all — either because the generator produced something the language rejects, or because nothing at all was recovered from source the compiler accepted.

## What the generator expects of its host

The compatibility generator accepts either a Roslyn `Compilation` or a Generation `DotNetProjectCompilation`. It never opens a project file or loads a workspace, which is what lets it be driven from a CLI, from a specification, or from an editor. `DotNetProjectCompilation` adds the host-owned project role, authored syntax trees, and stable source-path context needed by neutral source adapters; the established compilation-only overloads remain available and produce the same `.play` bytes. The other side of that bargain is that **assembling the compilation the way a real build would is the caller's job** — the generator reads what it is handed and cannot tell a missing type from a type that was never written.

Distinguish **workspace source generators** from **MSBuild-generated inputs**. `GetCompilationAsync` belongs to Roslyn `Project`, not `MSBuildWorkspace`. A workspace project compilation can already contain source-generator output; do not run the same generators over it again unconditionally. Arc's CLI uses `project.GetCompilationAsync(cancellationToken)` and separately restores missing resource sources/framework references where needed. MSBuild/custom-tool output such as resource designer files is not interchangeable with Roslyn generator output. Arc's TypeScript proxy generator is a post-build executable, not a C# source generator.

For an already loaded workspace `Project`, the host fragment is:

```csharp
using Microsoft.CodeAnalysis;

static Task<Compilation?> GetCompilation(Project project, CancellationToken cancellationToken) =>
    project.GetCompilationAsync(cancellationToken);
```

Handle a null compilation and inspect diagnostics before passing it to Screenplay. Loading the workspace, restoring packages, and supplying missing build-generated files remain host responsibilities.

If you intentionally construct a **raw C# compilation outside the workspace compilation path**, run the selected generators once. This alternative fragment assumes `project` supplies the matching analyzer references and parse options, and `compilation` has not already received those generated trees:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

static Compilation RunGenerators(Project project, Compilation compilation)
{
    var driver = CSharpGeneratorDriver.Create(
        generators: project.AnalyzerReferences
            .SelectMany(reference => reference.GetGenerators(LanguageNames.CSharp)),
        parseOptions: (CSharpParseOptions)project.ParseOptions!);

    driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var diagnostics);
    foreach (var diagnostic in diagnostics)
    {
        Console.Error.WriteLine(diagnostic);
    }

    return generated;
}
```

`AnalyzerReference.GetGenerators` already returns `ISourceGenerator` values. `AsSourceGenerator` converts an `IIncrementalGenerator`; applying it to those values does not compile. Real custom hosts must also supply any generator-required additional texts and analyzer-config options, and decide how diagnostics affect their exit status. This fragment is not a complete replacement for the CLI loader.

### What happens when it does not

Nothing is hidden, and nothing correct is thrown away. A compilation carrying errors is still analyzed, and `SP0024` says what happened — how many errors there were, the first of them, and how many artifacts came through anyway:

```text
Warning SP0024: The source did not compile - 607 error(s), the first being 'The name
'AccountsMessages' does not exist in the current context'. 341 artifact(s) were recovered
anyway, 341 of them from a declaration no error sits inside, so the document describes those
exactly as the source states them - a missing type named like 'SomethingMessages' or a
designer class usually means the compilation was handed over without the compile items a
build generates (Accounts)
```

Its **severity is decided rather than fixed**, because "the source did not compile" covers two outcomes that could not be further apart:

- **Warning** when at least one artifact was recovered from a declaration no compilation error sits inside. Those artifacts are described exactly as their source states them whatever failed elsewhere, so the run is successful and the document is worth keeping. A compilation missing its generated symbols lands here — the errors sit in code that declares no artifact, and the model is unaffected.
- **Error** when none were, either because nothing was recovered at all or because every declaration something came out of is one the compiler could not make sense of. There is then no part of the document a reader could trust, and a host following the contract exits non-zero.

A count is used rather than a proportion deliberately: any threshold would make the same recovery pass for a large application and fail for a small one, and zero is the only number that means recovery was _prevented_ rather than merely dented.

Either way the document is written out, so what was recovered can be read.

### An application written as several projects

Nothing says an application is one project. A layered one puts its contracts in a project of their own; a host sitting beside the bounded contexts it serves is two projects at least — and no single one of them describes the application. Pointed at any one, the generator would describe half of it and refer to events it never introduced.

So `Generate` takes a list of compilations as well as a single one, and a host generating from a solution hands over one per project:

```csharp
var result = generator.Generate([contracts, application, host], options);
```

A host that also runs neutral adapters should retain the project metadata instead of reducing each project to its compilation:

```csharp
IReadOnlyList<DotNetProjectCompilation> projects = LoadProjects();
var result = generator.Generate(projects, options);
```

This project-aware compatibility overload intentionally delegates to the same established generator. The source context is carried for the neutral adapter path, not used to reinterpret legacy placement.

What comes back is one document rather than one per project, because the boundaries between projects are a build concern rather than something the model has:

- A namespace two projects declare into is **one slice**, whichever project each artifact sits in.
- A concept is **declared once**, however many projects refer to it.
- An event a sibling project declares is one the application **has**, so it is declared like anything else — not imported the way an event from a referenced package outside the application is.

Paths stay readable because they are written relative to the directory all the projects sit under rather than to each project's own root, so every path opens with the project it belongs to — `Library/Shipping/Dispatching/Dispatching.cs` beside `Library.Contracts/Ordering/Placing/Placing.cs`. Projects checked out in unrelated places share no such directory; each one's paths then fall back to its own root, and `SP0038` says so, because two files can then come out as the same path.

The order the projects arrive in never reaches the document. Nothing decides what order a host enumerates a solution in, so they are sorted by assembly name before anything is read and the same projects always print the same bytes. Where that order has to decide something it says so: two projects declaring the same artifact name into one slice keep the first and report `SP0037`.

## Inline events and destinations

An event used by exactly one production site can appear as `produces event <Name>` inside its command. The generator counts production sites across every analyzed project, including reactors and other application code but excluding specification fixtures and `nameof` references. Runtime-typed appends and projects outside the analyzed set are not counted. Values whose event type is only known at runtime are not counted. It only inlines a local generation-one event in the same slice when the command has a required scalar identifier, the production is unconditional, and every payload member has a supported mapping. Tombstones, compensations, unsupported shapes, and payload copies of the identifier stay standalone.

Both forms preserve descriptions, documentation, rename pins, and the persisted payload shape. Inlining changes where the declaration appears, not its executable meaning. Scoped embedded documents still import an inline event declared outside their scope, and the board keeps its schema and flow links.

The generator emits `identifier` and explicit `for` destinations only when every production demonstrably uses command context. A routed wrapper, an unproven tuple destination, explicit append, or aggregate fetched for another identity keeps standalone productions without `for`; `SP0051` reports the unrepresented destination once per command, even when the command has no identifier, rather than retargeting it.

## Generated values and responses

Readable generated UUID concepts and command responses are emitted by default only for commands without successful scenarios. Until deterministic generation fixtures and response expectations are supported, commands with successful scenarios keep their legacy productions or handler reference without `generated` or `returns`; `SP0052` explains what was withheld to preserve those scenarios. A generated value must be a required scalar concept backed by `Uuid`, with no concept validator or validation rules. Its local must be written only by its initializer, and the resolved constructor must construct that same concept type and forward the fresh UUID unchanged to the concept or event-source base, without casts or user-defined conversions. Response-record fields must be compiler-synthesized positional properties, not explicit properties that transform their inputs. Scalar responses use `returns <property>` only for a direct value of the same declared type without a value-changing conversion; fully readable response records use a `returns` block whose fields refer directly to command inputs or admitted generated values. These constructs select ESM v7; documents without them retain their existing ESM version.

Generated values do not exist during authorization or validation. Property rules and requirements cannot reference them (`PLAY0273`); when recovered protection reads a generated value, the command falls back to its legacy representation without `generated` or `returns`, and `SP0052` explains that the protection remains in code. The generator's recovered authorization policies describe the caller, not generated command properties.

Unadmitted generated shapes and dependent responses are omitted with `SP0052`. A required event payload mapping that needs such a value also selects the legacy command representation: the production remains standalone, the unreadable mapping is omitted with `SP0012`, and the command and its specifications stay visible. This preserves the existing document even when the missing required mapping prevents executable binding. For an optional payload member, only the blocked mapping is omitted; the production can still bind. A production whose condition depends on an unadmitted value is omitted with an explicit diagnostic; when no production remains, the command keeps a handler reference. An unproven tuple destination reports `SP0013`.

A generated identifier supplies an inline event's destination. Standalone productions using that identity carry explicit `for <identifier>`; a plain production without `for` still needs its separate allocation channel. Empty and provably response-only commands need no handler. Commands whose behavior lives in code keep a `handler` file reference by default, as before, rather than disappearing from the embedded board or being described as recording no facts. A command never carries both `produces` and `handler`.

The generator preserves the legacy authoring document and adds generated values and responses only where they are admitted. Documents containing handler references still compile and round-trip, but have **no executable model** because handlers report `PLAY0268`.

## Including authoring-only constructs

Set `ScreenplayOptions.AuthoringOnlyConstructs` to `true` when you want a fuller authoring document rather than an executable model. It defaults to `false`. Embedded generation exposes the same boolean on `EmbeddedDocumentOptions`; MSBuild projects can set `CratisEmbeddedScreenplayAuthoringOnlyConstructs` to `true`. The separately shipped CLI must expose the option before it can be selected there.

The option retains handler references even for response-only commands, and adds returned command operations with their external system and execute/compensate implementation files, and event-source/stream declarations with property-backed command routes. It also retains generated authoring shapes outside the executable subset. An operation must use exactly one external system in the current grammar. Existing, value-bearing concurrency dimensions remain unchanged; observer filters and dynamic concurrency flags without a value are still reported rather than invented.

It also describes keyed read-model dependencies of `Provide()` and `Handle()`, simple provisioning rejection comparisons as acceptance requirements, and event mappings from read-model members. Reads use the command's proven event-source key, matching Arc's dependency resolution. Arbitrary provisioning stays in code and is reported; no `provide` block is generated.

These documents still compile and round-trip, but **there is no executable model while `PLAY0268` constructs are present**. Binding legacy reads also reports `PLAY0271` because they cannot imply decision consistency. Enabling the option does not execute operations, generate implementation code, or weaken those admission checks. Unsupported types, ambiguous names and unreadable behavior retain diagnostics instead of guessed output.

## The generator checks its own output

Every diagnostic above names something about _your application_ — a construct the language cannot hold, source that did not compile, projects that share no directory. There is one that names a defect in the generator instead.

After the document is written, the generator hands it straight back to the Screenplay compiler. If the compiler rejects it, `SP0034` is reported as an error — because a `.play` that does not compile is output nobody can use, and there is no way of writing an application that avoids it. This is not a mode you turn on: it runs on every generation, since the only way a rejected document is ever found is by reading each one back.

```text
Error SP0034: The generated document did not compile - 1 error(s), the first being
'Invalid description 'description RequestDescription' - expected 'description "<text>"''
on line 6. That is the generator being wrong rather than anything the source declared,
and the document is returned as it stands so the line can be read (Library)
```

The document is still written out, so you can open it at the reported line and see what happened. If you hit this, it is a bug worth [reporting](https://github.com/Cratis/Arc/issues) — include the line, and the C# declaration it came from.

Source that did not compile (`SP0024`) suppresses `SP0034` **when it is reported as an error** — a model recovered from symbols the compiler never accepted describes an application that does not exist, so a poor document made from it is a consequence of the broken build rather than a second, separate defect. Fix the build and generate again.

As a warning it suppresses nothing. That severity says the model stands, and a document built from a model that stands is exactly what the check exists for — suppressing it there would hand back a `.play` the language rejects with nothing wrong reported.

## A value is only ever what the source states

Wherever the document states a value — the mapping a command's `produces` writes into an event, the values a scenario is issued with, the message a validation rule fails with — it states what the source states and nothing beyond it. Two sources survive: a constant the compiler already holds, and a path into the command's own input. Anything arrived at while the request runs is left out and reported, because a guessed value is worse than an absent one.

Messages are where that bites. An application speaking more than one language declares each message once in a resource and names it from the validator, and the property it names resolves its text against the caller's culture — so there is no text for the compiler to hold, and no single text the document could honestly state.

The key is there to be read even though the text is not, and it is the better of the two to take: text would settle the document on one language the application itself never settled on. So the key is what is written, unresolved:

```text
command RequestBook
  title String
  validate
    title not empty message $strings.RequestMessages.TitleRequired
```

A key is qualified by the class declaring it, because a key is unique to its own resource and to nothing wider. Two areas of one application both requiring an organization number is ordinary rather than a mistake, and bare, those two would be one key that can carry only one text.

A message genuinely put together in code — `string.Format`, an interpolation — still has no text to write down, and is reported as `SP0016` rather than guessed at. So is a key the language has no way of writing: a reference is a path of bare words, and a resource key is under no such constraint.

## Identifiers, event documentation, and named rules

A command's required scalar event-source key is emitted with `identifier`, and its productions carry `for <property>`. The generator follows Chronicle's key rules: a property assignable to `EventSourceId`, a generic `EventSourceId<T>`, or `[Key]` on the property or its matching constructor parameter. An implicit conversion from a `ConceptAs<Guid>` to `EventSourceId` does not make that concept a key. An `ICanProvideEventSourceId` implementation is recovered only when it directly returns a required scalar property.

Several candidates produce `SP0049`; an unreadable or optional identity produces `SP0050`. In either case the command remains, but no identifier or destination is guessed.

An event's XML `<summary>` becomes its `description`; `<remarks>` becomes a Markdown documentation block. References such as `<see cref="AuthorId"/>` retain their names in backticks. Remarks containing a Markdown fence are left out with `SP0014`, because the fence cannot be nested. A generation-1 `[EventType("PreviousName")]` whose persisted name differs from the current type name emits `id "PreviousName"`. Later generations still report `SP0014` and do not emit a rename pin.

A top-level property rule written as `.Must(IsKnownName)` becomes a named `rule IsKnownName` with a repository-relative `file` referencing the predicate's source. Its method name is preserved. Predicates without a portable implementation file, inline lambdas, and unsupported rule shapes remain omitted with `SP0016`; a message following an omitted rule does not attach to the rule before it.

## The scenarios a slice is specified by

A `.play` says what a slice does. The Chronicle integration specs in the folder beneath it already say the same thing by example — what had happened, the command that was issued, what followed — which is exactly the shape of a Screenplay `specification`. So they are read too, and the document carries the examples proving the model rather than only the model:

```text
slice StateChange Registration

  command RegisterAuthor
    name String
    produces AuthorRegistered
      name = name

  event AuthorRegistered
    name String

  specification WhenRegisteringAndTheNameIsTaken
    given AuthorRegistered
      name = "Jane Austen"
    given readmodel Author
      id = "author"
      name = "Jane Austen"
    when RegisterAuthor
      name = "Jane Austen"
    then error "unique-author-name"
```

- **`given`** is what the specification started from — the events it seeded, and the read model it pinned.
- **`when`** is its action: a command it executed, or `when append <EventType>` for an event scenario's append.
- **`then`** is each event it asserted was appended, and **`then error`** a rejection it asserted.

Both command-testing shapes Arc documents are read: the in-process one driving the pipeline through a scenario (`Scenario.Given…`, `Scenario.Execute`) and the one driving a running host (`EventLog.Append`, `Client.ExecuteCommand`). Event scenarios are also read: `EventScenario.When.ForEventSource(...).Events(...)` or a direct append to its event sequence becomes `when append`, not a command. An append action must state one event. Which calls are which is decided by the type each one sits on, so neither testing package has to be referenced for either to be read.

Concrete event-scenario source arguments are emitted as indented `for` values on `given`, `when append`, and event `then` blocks, separately from payload properties, only when every producing command retains the same required scalar identifier type and the values fit that type. A `Uuid` destination requires a canonical lowercase GUID string. When a source cannot be typed this way, a scenario sharing one source remains implicit; distinct sources take the scenario out with `SP0039`. A shared symbolic source can also remain implicit. Sources that are neither provably the same symbol nor concrete values the document can state take the scenario out with `SP0039`; separate calls to `EventSourceId.New()` are not the same source.

A rejection the source asserts without naming a reason is written as bare `then error`. The source gives no code or presentation message, and inventing either would put meaning in the document the application never states.

The generator does not yet recover `then returns` expectations. When a command does not emit `returns`, assertions against `CommandResult.Response` are ignored while the scenario's other outcomes are retained, as in the legacy document. Response-only scenarios remain omitted with `SP0039`. If the emitted command has `returns`, a scenario asserting its response is omitted rather than emitting a partial expectation. In default mode, successful scenarios cause the command's generated values and responses to be withheld with `SP0052`, preserving its legacy productions and scenarios until fixtures are supported. Screenplay requires indented `for <value>` for a generated identifier and `generated <property> = <value>` for other generated values beneath `when`; missing fixtures would execute as `Unsupported(IdentityAllocation)`, not a passing example. Rejection scenarios need no generation fixture because validation precedes generation.

Expect the document to grow. On a real application this roughly doubled it, at about seven lines per scenario.

### A scenario is read whole or not at all

A mapping stands on its own, so one that cannot be read can be left out while the rest of its block still says something true. A scenario cannot: an example missing the state it started from, the action it performed, or the outcome it expected is not that example — it is a different one nobody wrote. So a step that cannot be read takes the whole scenario with it, and `SP0039` names the scenario and what made it unreadable.

A value the document cannot hold takes the scenario with it the same way. The document names an enumeration member, and a scenario value that is none — a number cast to an enumeration that declares no member with it, `default` for an enumeration with no zero member, `null` for a required record or list, or several `[Flags]` members combined into a value no member is declared with, because Screenplay has no form to state combined flags — would be written as a different example if it were dropped or invented. `SP0039` names the property, the value and why.

Conditional steps are the common case. Anything written under an `if`, a `switch`, a loop, a ternary or a lambda happened in some runs of the specification and not in others, and the source text does not say which — so a step recovered as unconditional would state a world nobody specified. That is the one failure mode a reader has no way of catching, which is why it is reported rather than guessed at.

A step need not construct what it states where it is written. A specification routinely holds the event or the command in a member and names that member in the step — `Scenario.Given.ForEventSource(id).ReadModel(TargetUser)`, `Scenario.Execute(_command)` — because the same value is asserted on later, or because it is built where the values it needs already are. Such a member is followed **one hop**, to the single place it was put together, and the step reads exactly as the inline form does. A `= null!` or `= default` declaration is not one of those places: it exists to satisfy the compiler and states no value.

One hop, and only from one place. A member given a value twice, or given it under a condition, held different values in different runs and the source does not say which one the step saw — so it stays unread and the scenario is left out with `SP0039`, the same as any other conditional step. Following a chain would mean reasoning about what a value was at the moment the step ran, which is a different discipline from reading what was written.

Values are the exception for the compatibility document generator, because a value stands on its own the way a mapping does. They follow [the discipline every other value follows](#a-value-is-only-ever-what-the-source-states): the identity two steps agree on is routinely a fresh identifier held in a field rather than something written down, and such a value is reported on its own while the rest of the legacy scenario stands.

### Neutral specification facts are stricter

`ArcSpecificationFactAdapter` is the independently consumable source-evidence surface. It contributes Generation scenario, ordered-step, and typed-value facts only when every explicitly authored step and value is exact. One computed or unreadable required value, conditional/repeated step, unretained read-model assertion, or event predicate value blocks the whole neutral scenario with `ARCSP0001`; it never contributes a smaller example than the source wrote.

The adapter retains scenario-, step-, value-, and rejection-level source ranges separately from the existing Arc model. This does not change legacy model equality or the current `.play` generator's compatibility output. It creates one fixed `DotNetSourceStructures` snapshot and asks `DotNetSourcePlacementDerivation` to place all exact targets together. Application and specification projects therefore use the application target's source placement, read-model and query targets retain `StateView` placement, and folder/namespace disagreement or invalid source policy produces a typed `DOTNETSP####` diagnostic instead of a guessed placement. Generation attaches a scenario only through that exact target placement and lowers it only after complete atomic admission.

This distinction is deliberate: the compatibility generator keeps existing consumers stable, while the neutral adapter provides the fail-closed evidence required for render→recover semantic fidelity.

## Screens

A [vertical slice](../../vertical-slices.md) puts the React component that realizes a slice's screen in the same folder as its C#. Roslyn syntax trees carry real file paths, so the generator knows where each slice's source lives and declares a `screen` for every `.tsx` component sitting next to it, named after the file:

```text
slice StateView Listing
  query AllAuthors => Author[]
  query AuthorById(id : String) => Author

  screen AuthorList
    file Authors/Listing/AuthorList.tsx
    data Author[] via query AllAuthors
    data Author via query AuthorById by id
```

Two things about a screen are recovered, and both come from something that can be checked.

**The `file` reference** says which file realizes the screen. It is what a reader opens, and no directive replaces it, so it stays on the screen even when directives sit beside it.

**The `data` directives** say which of the slice's queries the screen reads through. Arc generates a TypeScript proxy per query and a component imports that proxy by name, so the component's `import` statements name candidates — and a candidate is kept only when it matches a query the slice really declares. Nothing about the binding comes from the component beyond that name: the read model, whether there is one or many of it, and the parameter it is keyed by all come from the C# query. An import naming anything else — a package, a command, a sibling component, a type-only import — leaves nothing behind.

Everything else in the declarative form — `title`, `section`, `table` and `summary` with their columns and fields, `action`, `navigate to`, `layout` — is **never inferred**. That is structure expressed in JSX and component properties, and a guessed column is worse than an absent one: it puts a confident falsehood into a document whose entire value is that it describes the real application. Every screen reports `SP0028` to say so. Write those directives by hand if you want them, and expect a regeneration to leave them out.

Two more rules keep the result honest:

- **Components only, no descending.** A file carrying a second extension — `AddAuthor.stories.tsx`, `AddAuthor.spec.tsx` — is a companion of a component, not a screen. A folder _inside_ a slice folder is a slice of its own under the same convention, so its files belong to it.
- **Anything uncertain is reported.** The relationship between a file and a slice comes entirely from where the file sits, so `SP0025` is reported whenever sitting there says less than usual: a slice whose source is spread over several folders, one folder holding the source of several slices, or two files claiming a single screen name.

## What is not expressed

A `.play` is a description of an application, not a second copy of it. **It does not round-trip back to code**, and reading one will not tell you everything the code does. The losses run in both directions.

### Screenplay constructs the generator cannot infer

These are part of the language, but nothing in C# says them, so a generated document never contains them. Add them by hand if you want them, and expect a regeneration to leave them out:

| Construct                                                                                                      | Why it cannot be inferred                                                                                                                               |
| -------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `capture`                                                                                                      | Describes ingesting an external system. Nothing in an Arc application declares one.                                                                     |
| `persona`                                                                                                      | Who uses the system is a product decision, not a code artifact.                                                                                         |
| `seed`                                                                                                         | Sample data is a modeling concern, not something the source states.                                                                                     |
| The declarative body of a `screen` — `title`, `section`, `table`, `summary`, `action`, `navigate to`, `layout` | What a screen _shows and does_ is JSX. Its `file` reference and its `data` bindings are generated; the rest would be a guess — see [Screens](#screens). |
| `@sensitive`                                                                                                   | `@pii` is the one of the two concept attributes with a counterpart — `[PII]`. Nothing in Arc or Chronicle says `@sensitive`.                            |

### Detail Screenplay cannot represent

These details can remain outside the document because the language has no counterpart, the source cannot be read reliably, or an authoring-only construct is disabled. Diagnostics explain what the document leaves out.

| In Arc or Chronicle                                                                                   | Why it is not in the document                                                                                                                                                                                                                                                                                                                                                                                                                       |
| ----------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Event generations, `[Tombstone]`, `[CompensationFor]`                                                 | Screenplay describes the current shape of an event. It has no notion of versioning, of a deletion marker, or of one event compensating another. `SP0014`.                                                                                                                                                                                                                                                                                           |
| Reducer folds (`IReducerFor<T>`)                                                                      | The fold is code. The read model and the events it observes are recovered; the logic that combines them is not. `SP0020`.                                                                                                                                                                                                                                                                                                                           |
| `[FromEventSource<TSource>(stream)]` on a reactor or reducer | The source and stream the observer is filtered to are recovered and kept in the analysis model, but the latest published Screenplay syntax (`Cratis.Screenplay` 4.68.2) gives a reaction trigger and a projection no way to narrow them to an event source or stream. The observer is emitted observing its events from every source, so `SP0047` says what the document leaves out, and `SP0048` says when the definition does not declare the stream. |
| Aggregate roots no command reaches                                                                    | The events an aggregate root applies are stated through the command that hands its work to it. One that nothing calls has nothing to state them through — a document has no construct for a class that decides on its own. `SP0018`.                                                                                                                                                                                                                |
| A behavior deciding on the state an aggregate root holds                                              | A `produces when` condition compares the input of the command, which is all a document knows at the moment the command is issued. A behavior refusing to act on what it has already seen is a real decision with nowhere to go, so the event is stated unconditionally and `SP0027` reports the decision. A behavior deciding on one of its own _parameters_ is recovered, because the call site says which command input that parameter was given. |
| Inline `policy` code and requirements built in code                                                   | `RequireAssertion(…)` and a policy registered from an `AuthorizationPolicy` built elsewhere are code. `RequireAuthenticatedUser`, `RequireRole`, and `RequireClaim` given the values it accepts are recovered; the rest is reported as `SP0026` — including a `RequireClaim` naming only a claim type, which a policy condition has no way to state.                                                                                                |
| The event source id from a `(TKey, TEvent)` handler                                                   | The event is recovered. An admitted generated UUID identity and response are stated by default. If the destination cannot be proven or the generated concept has validation, no destination is inferred and `SP0013` explains the omission.                                                                                                                                                                                                                                                                                                                                         |
| Emptying a scope with `[ClearWith]`; removing a child with `[RemovedWith]` on the property holding it | Nothing in the model a projection is built from carries a scope being emptied again, so `[ClearWith]` has nowhere to go (`SP0015`). A removal does have somewhere — but it is read from the type of the child, alongside the events filling that child in, so the same removal written beside the collection is reported as `SP0007` instead.                                                                                                       |
| Read model tags                                                                                       | A read model has no declaration of its own — it appears as the type a query returns — so there is nowhere to hang a tag. Tags on _events_ are recovered and written out. `SP0042`.                                                                                                                                                                                                                                                                  |
| Query paging and sorting, custom routes                                                               | These say how a model is served rather than what it is. The parameters the host fills in are left out, and a route template — `[Path]`, `[Route]`, or a template on an HTTP verb — has no counterpart. `SP0041`.                                                                                                                                                                                                                                    |
| Several command identity candidates | The generator cannot choose between key properties or event source identities. No `identifier` is emitted. Information diagnostic `SP0049`. |
| An unreadable command identity | A self-provided identity must directly return a required scalar command property; an optional or collection key cannot be an identifier. Information diagnostic `SP0050`. |
| An explicit or unprovable production destination | A production cannot be proven to use the command's event source. Its `for` destination and the command's identifier modifier are omitted rather than guessed. Information diagnostic `SP0051`. |
| Generated values and responses outside supported shapes | Admitted values and responses are emitted by default. A generated value needs a required scalar UUID concept with no validator or rules; a response must be a direct property or fully readable record. Unsupported shapes and pre-generation references remain in code. Information diagnostic `SP0052`. |
| Unreadable returned operations | Operations are authoring-only. Unsupported construction, input mappings, batch spreads, or ambiguous external systems remain in code; readable literal batch members can still be described. Information diagnostic `SP0053`. |
| Event-source definition routes | Source and stream syntax is authoring-only. `SP0044` is Information when the option is disabled and Warning when it is enabled but no unambiguous readable route can be stated. Existing concurrency dimensions are retained. |
| Unreadable source or stream routes | Routes are authoring-only. Computed stream ids or conflicting source and stream declarations cannot be stated without guessing. Information diagnostic `SP0054`. |
| Unreadable reads or provisioning | Reads and requirements are authoring-only. Dependencies need a proven input key and a uniquely readable model and projection; only supported comparison guards become requirements. Other provisioning remains in code. Information diagnostic `SP0055`. |

If a generated `.play` is missing something you expected, the diagnostics are the first place to look — the omission is almost always reported.

## When this is the wrong fit

If you maintain a `.play` by hand as the _design_ your code is written against — modeling first, then implementing — generation is the wrong direction and will overwrite your intent. Generation suits the opposite flow: code exists, and you want the model it already describes, kept honest automatically.

## Related

- [Embedded event-model explorer](embedded-event-model.md) — generate documents during the build and browse them inside your ASP.NET Core application.
- [Vertical slices](../../vertical-slices.md) — the folder shape the generator recovers slices from. A slice per namespace produces a far better document than artifacts sitting in the root namespace.
- [Understanding the proxy boundary](/arc/understanding-the-proxy-boundary/) — the other thing Arc generates from the same source of truth.
