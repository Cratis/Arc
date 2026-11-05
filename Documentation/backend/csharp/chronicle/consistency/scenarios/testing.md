---
title: Cratis Arc specs for state-dependent commands
description: Give a Cratis Arc command known state, assert its event or rejection, and check read admission without simulating concurrent writers.
---

Specify a state-dependent command in Cratis Arc by giving it known state and checking the event it appends or the rejection it returns. Use `CommandScenario<T>` when the rule depends on validation, parameter binding or appending events. A direct `Handle(state)` call is enough only when the decision you want to specify lives in that method.

The scenario selects its read path from the command. Declare `[ReadProtection(ReadProtectionMode.On)]`, or enable `On` for the command's assembly, where the rule must hold under concurrency. Application specs do not configure decision reads or simulate competing appends; Arc and Chronicle verify guard handling in their framework specs.

## Give a rule its state

For a borrowing rule, start with one available copy and assert both success and the recorded loan. This illustrative fragment assumes a `BorrowBook(EventSourceId)` command with a protected plain `BookStock` parameter, a model `BookStock(EventSourceId Id, int Available)` and a registered `BookBorrowed` event. Use your application's command constructor and model shape when adapting it:

```csharp
public class when_borrowing_an_available_book : Specification
{
    readonly EventSourceId _book = EventSourceId.New();
    readonly CommandScenario<BorrowBook> _scenario = new();
    CommandResult _result = default!;

    void Establish() => _scenario.Given.ForEventSource(_book)
        .ReadModel(new BookStock(_book, 1));

    async Task Because() => _result = await _scenario.Execute(new BorrowBook(_book));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] async Task should_record_the_loan() =>
        await _scenario.ShouldHaveAppendedEvent<BorrowBook, BookBorrowed>(_book);

    async Task Destroy() => await _scenario.DisposeAsync();
}
```

`.ReadModel(...)` pins a known value for this model and source. The pin reaches the plain parameter after Chronicle performs the real protected read: it still checks whether the model supports protection, enrolls the read and obtains the token used for conflict detection. The pin supplies the value rather than fabricating a guard. It also works when the validator constructor takes the plain model.

Add a separate spec with zero available copies. Assert the rule's validation message and that the scenario's `AppendedEvents` is empty. Under read protection, that collection contains successful command appends, not seeded history. Testing only `ShouldBeSuccessful()` would miss a command that reports success without recording the loan.

Keep a missing required model separate from an unavailable book. A non-nullable dependency with no instance can reject the command before the business rule runs; see [dependency unavailable versus business-rule rejection](../../../testing/command-scenario.md#dependency-unavailable-is-not-a-business-rule-rejection).

For a per-stream model, seed events on the command's route rather than using a source-only pin. An explicit `DecisionRead<T>` also needs events; pins cannot supply its decision token. Follow [Testing Chronicle commands](../../../testing/chronicle.md#give-a-protected-read-model-its-state) for the supported state-giving options.

## Check admission

Before relying on those outcome specs, check that Chronicle can protect the models your commands declare. `DecisionReadAdmissions.ShouldAdmitDecisionReadsOf` checks selected commands; `ShouldAdmitDecisionReadsIn` scans assemblies. Both inspect declared protected parameters and discoverable validator dependencies without executing commands or requiring a running Chronicle.

A passing check means the discovered models are admitted for those declared reads. It does not execute the business rule or discover explicit `IDecisionReads.Get` calls inside a method body. The helper ignores any runtime cap that lowers read protection, so deployment settings cannot hide a declared refusal. See [Check that Chronicle admits the decision reads](../../../testing/chronicle.md#check-that-chronicle-admits-the-decision-reads) to add the assertion.

:::note[Advanced: framework verification]
The framework's own specs simulate competing writers to prove guard handling. A caller with a specific, explicit need can use the hidden concurrent-append hook described in [Chronicle testing](../../../testing/chronicle.md#protected-decision-scenarios). This is not the recommended application testing surface.
:::

## Related

- [Getting started](../getting-started.md)
- [Testing Chronicle commands](../../../testing/chronicle.md)
