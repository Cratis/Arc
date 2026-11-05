---
uid: Arc.Testing
title: Choose a test boundary in Cratis Arc
description: Choose direct specs for command decisions, Arc scenarios for pipeline behavior, and integration tests for real hosts, providers, and application flows.
---

Test a Cratis Arc command's decision by calling `Handle()` directly. Use `CommandScenario<TCommand>` to prove that Arc validates input, supplies dependencies, and executes the handler through its pipeline. Hosted or provider-backed integration tests cover the real infrastructure that these specs do not exercise.

Choose the boundary around the failure you need to catch. A wrong calculation belongs in a direct spec; a validator that never runs needs a pipeline spec. For queries, use [query scenarios](./query-scenario.md) to check read-model binding, filters, or authorization, and [observable query scenarios](./query-scenario.md#observable-queries) to assert initial and subsequent emissions.

## Choose the boundary that can catch the bug

| What you need to prove                                                     | Start with                                                       | What it does not prove                                            |
| -------------------------------------------------------------------------- | ---------------------------------------------------------------- | ----------------------------------------------------------------- |
| A deterministic calculation or decision                                    | Direct `Handle()` spec with explicit input/state                 | Arc validation, authorization, `Provide()`, or persistence        |
| Returned events, rejections, or validator messages                         | [Command and validator assertions](./command-assertions.md)       | Pipeline composition or actual event appends                      |
| Calls to an application service                                            | Direct unit spec with substituted collaborators                  | The real service, database, or production registration            |
| Validation, authorization, `Provide()`, dependencies, or command responses | `CommandScenario<TCommand>`                                      | HTTP routing/authentication middleware or external infrastructure |
| Model-bound query binding, validation, authorization, or buffered observable emissions | `QueryScenario<TReadModel>` | HTTP routing, scheme authentication, streaming transports, or real database watchers |
| Returned operations execute and compensate in the correct order           | `CommandScenario<TCommand>` with controlled provider dependencies | Production provider idempotency, terminal cancellation, or crash recovery |
| Returned events use the intended source and append contract                | `CommandScenario<TCommand>` with the Chronicle testing extension | Completion of the application's entire observer/reactor flow      |
| Projection/reducer state from history                                      | Chronicle read-model scenario                                    | Production sink configuration and transport                       |
| Actual host, provider, or asynchronous application flow                    | Hosted/provider-backed integration spec                          | Every business edge case unless you explicitly cover it           |

Cover decision branches with direct specs, then add scenario specs for the Arc contracts those calls bypass. Keep a smaller set of integration tests for real composition. That way, a calculation change does not need a running host to tell you it is wrong. Keep the action in `Because()`, give every `should_...` assertion a concrete outcome, and wait for facts rather than sleeping.

Start with [the decision-and-pipeline lesson](./command-decisions.md). It tests the same strongly typed command both ways. For inline side effects, [test command operations](./command-operations.md): inspect declarations directly, then let `CommandScenario` run actual execution and compensation with substitutes. For event-sourced applications, [the Chronicle lesson](./event-sourced-commands.md) shows why a correct returned event is not enough: the appended identity must be correct too.

`Provide()` can acquire data while `Handle()` expresses the decision. A direct spec supplies that data itself; a scenario checks that Arc supplies it. Neither needs an application-specific handler abstraction or a test-only execution path. A handler can also call services directly, but a method with I/O or mutable external state is not a pure function merely because it lives on a command.

For pipeline specs, start with `Cratis.Arc.Testing`. It has no Cratis Chronicle dependency. Your handlers still need their application services; register test implementations or real dependencies as appropriate. The scenario does not automatically replace external databases or services your code uses.

Generated values are another explicit choice: command scenarios fail an unpinned generated parameter by default. [Pin generated values](./generated-values.md) when the exact value matters, or call `AllowGeneration()` before execution to exercise the factory. A fixed clock controls time, not the random part of an identifier.

## Packages

| Package                        | Purpose                                                                            |
| ------------------------------ | ---------------------------------------------------------------------------------- |
| `Cratis.Specifications.XUnit`  | BDD-style `Specification` base class and assertions on top of xUnit                |
| `Cratis.Arc.Testing`           | Command and query scenarios without event sourcing                                |
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

The scenario builds its service provider lazily on the first `Execute` or `Validate` call. Register application dependencies through `Services` before that point, usually in `Establish()`. Configure a fixed clock before either call too; pins, generation policy, and tenant selection must be set before the first execution. See the [generated-value configuration rules](./generated-values.md#finish-configuration-before-the-first-execution) if a spec validates before executing.

Use `Validate` to run the pipeline filters without calling `Provide()` or `Handle()`. It does not generate values.

Continue with [command scenarios](./command-scenario.md) for validation, authorization, assertion contracts, and disposal.

<a id="3-add-chronicle-assertions-when-the-command-appends-events"></a>

## Optional: Test event-sourced commands

Only commands using the Chronicle integration need `Cratis.Arc.Chronicle.Testing`. Its discovered extender lets you seed state and inspect appended events without an external Chronicle server. Ordinary event-log scenarios expose `Given`, `EventScenario`, `EventLog`, `EventSequence`, and `AppendedEvents`; commands with protected dependencies use the decision store instead.

See the [optional Chronicle testing extension](./chronicle.md) for setup, the distinction between event-log and read-model seeding, and event assertions. Keep the standalone package when you do not use event sourcing. The broader [Testing with Cratis guide](/testing-with-cratis/) connects Arc scenarios to the other product-specific test helpers.
