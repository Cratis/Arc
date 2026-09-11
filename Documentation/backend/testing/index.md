---
uid: Arc.Testing
title: Testing
description: Test standalone Arc commands through the real pipeline, with optional Chronicle event-sourcing support.
---

A useful spec answers a precise question: is the decision right, did Arc enforce the rule, or did the real system persist and serve the intended result? Cratis lets you test each of those without forcing every case through the widest—and slowest—boundary.

A command's `Handle()` is an ordinary C# method. Call it directly for a deterministic decision; use `CommandScenario<TCommand>` when the framework's composition is part of what you need to prove. Add hosted or provider-backed tests for boundaries neither can cover.

## Choose the boundary that can catch the bug

| What you need to prove                                                     | Start with                                                       | What it does not prove                                            |
| -------------------------------------------------------------------------- | ---------------------------------------------------------------- | ----------------------------------------------------------------- |
| A deterministic calculation or decision                                    | Direct `Handle()` spec with explicit input/state                 | Arc validation, authorization, `Provide()`, or persistence        |
| Calls to an application service                                            | Direct unit spec with substituted collaborators                  | The real service, database, or production registration            |
| Validation, authorization, `Provide()`, dependencies, or command responses | `CommandScenario<TCommand>`                                      | HTTP routing/authentication middleware or external infrastructure |
| Returned operations execute and compensate in the correct order           | `CommandScenario<TCommand>` with controlled provider dependencies | Production provider idempotency, terminal cancellation, or crash recovery |
| Returned events use the intended source and append contract                | `CommandScenario<TCommand>` with the Chronicle testing extension | Completion of the application's entire observer/reactor flow      |
| Projection/reducer state from history                                      | Chronicle read-model scenario                                    | Production sink configuration and transport                       |
| Actual host, provider, or asynchronous application flow                    | Hosted/provider-backed integration spec                          | Every business edge case unless you explicitly cover it           |

**Our recommendation: combine boundaries, rather than choose one for every test.** Cover decision branches with fast direct specs, add focused scenario specs for the important Arc contracts, and use a smaller set of integration tests to prove real composition. Keep the action in `Because()`, give every `should_...` assertion a concrete outcome, and wait for facts rather than sleeping.

Start with [the decision-and-pipeline lesson](./command-decisions.md). It tests the same strongly typed command both ways. For inline side effects, [test command operations](./command-operations.md): inspect declarations directly, then let `CommandScenario` run actual execution and compensation with substitutes. For event-sourced applications, [the Chronicle lesson](./event-sourced-commands.md) shows why a correct returned event is not enough: the appended identity must be correct too.

This separation is one of Arc's useful design properties. `Provide()` can acquire data while `Handle()` expresses the decision, without an application-specific handler abstraction or a test-only execution path. A service-backed `Handle()` is also supported; it is testable but is not a pure function merely because it lives on a command.

For pipeline specs, start with `Cratis.Arc.Testing`. It has no Chronicle dependency. Your handlers still need their application services; register test implementations or real dependencies as appropriate. The scenario does not automatically replace external databases or services your code uses.

## Packages

| Package                        | Purpose                                                                            |
| ------------------------------ | ---------------------------------------------------------------------------------- |
| `Cratis.Specifications.XUnit`  | BDD-style `Specification` base class and assertions on top of xUnit                |
| `Cratis.Arc.Testing`           | `CommandScenario<TCommand>` and `CommandResult` assertions, without event sourcing |
| `Cratis.Arc.Chronicle.Testing` | Optional in-process Chronicle extension for event-sourced commands                 |
| `Cratis.Testing`               | Optional convenience meta-package for projects using both Arc and Chronicle        |

<a id="quick-start"></a>
<a id="1-add-the-package"></a>

## Try a standalone command spec

In an existing xUnit test project, add the focused packages. Use versions compatible with your application's Arc packages:

```bash
dotnet add package Cratis.Specifications.XUnit
dotnet add package Cratis.Arc.Testing
```

<a id="2-write-a-spec"></a>

The following is a complete test file. It uses [Cratis Specifications](/testing-with-cratis/): with the default xUnit runner, each `[Fact]` gets a fresh spec instance, `Because()` performs the action before that fact, and `Destroy()` cleans up afterward. No database or Chronicle setup is needed.

```csharp
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Xunit;

namespace CommandSpecs;

[Command]
public record NormalizeName(string Name)
{
    public string Handle() => Name.Trim();
}

public class when_normalizing_a_name : Specification
{
    readonly CommandScenario<NormalizeName> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new NormalizeName(" Ada "));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] void should_return_the_normalized_name() =>
        ((CommandResult<string>)_result).Response.ShouldEqual("Ada");

    void Destroy() => _scenario.Dispose();
}
```

Run `dotnet test`. Both facts should pass: the result succeeds and its response is `"Ada"`. The handler returns an ordinary string, not an event. This checkpoint tests the pipeline and handler, not routing, HTTP serialization, or middleware.

<a id="topics"></a>

## Add validation and dependencies

The scenario builds its service provider lazily on the first `Execute` or `Validate` call. Register application dependencies through `Services` before that point, usually in `Establish()`. Use `Validate` to run the pipeline filters without calling `Provide()` or `Handle()`.

Continue with [command scenarios](./command-scenario.md) for validation, authorization, assertion contracts, and disposal.

<a id="3-add-chronicle-assertions-when-the-command-appends-events"></a>

## Optional: Test event-sourced commands

Only commands using the Chronicle integration need `Cratis.Arc.Chronicle.Testing`. When its extender is discovered, the same scenario gains `Given`, `EventScenario`, `EventLog`, `EventSequence`, and `AppendedEvents`. You can seed state and inspect appended events without an external Chronicle server.

See the [optional Chronicle testing extension](./chronicle.md) for setup, the distinction between event-log and read-model seeding, and event assertions. Keep the standalone package when you do not use event sourcing. The broader [Testing with Cratis guide](/testing-with-cratis/) connects Arc scenarios to the other product-specific test helpers.
