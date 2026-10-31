---
title: Specify a state-dependent rule
description: Give a command its read-model state and assert its outcome, without configuring decision reads or simulating concurrency.
---

Application specs give state and assert outcomes. Call `Handle(state)` directly to specify a decision. Use `CommandScenario<T>` when validation, parameter binding or event appending contributes to the behavior being specified. The scenario selects its read path from the command; specs never configure decision reads or simulate competing appends.

Arc and Chronicle prove their concurrency guarantees in their own framework specs. An application declares `[ReadProtection(ReadProtectionMode.On)]` where a rule must hold under concurrency; it does not re-prove the framework's guard implementation in every slice.

## Give a rule its state

This illustrative fragment assumes a `BorrowBook` command with a protected plain `BookStock` parameter, a model `BookStock(EventSourceId Id, int Available)` and a registered `BookBorrowed` event:

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

A pin reaches the plain parameter after the real protected read has been issued. Admission, enrollment and conflict detection remain unchanged. Give unavailable state in a separate spec to assert the rule's rejection. For a per-stream model, give routed events instead of a source-only pin.

## Check admission

`DecisionReadAdmissions.ShouldAdmitDecisionReadsOf` and `ShouldAdmitDecisionReadsIn` check declared protected parameters and validator dependencies without executing the command. The helper deliberately ignores the runtime cap so deployment settings cannot hide a declared refusal. See [Check that Chronicle admits the decision reads](../../../testing/chronicle.md#check-that-chronicle-admits-the-decision-reads).

:::note[Advanced: framework verification]
The framework's own specs simulate competing writers to prove guard handling. A caller with a specific, explicit need can use the hidden concurrent-append hook described in [Chronicle testing](../../../testing/chronicle.md#protected-decision-scenarios). This is not the recommended application testing surface.
:::

## Related

- [Getting started](../getting-started.md)
- [Testing Chronicle commands](../../../testing/chronicle.md)
