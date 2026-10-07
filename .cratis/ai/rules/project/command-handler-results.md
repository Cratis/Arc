---
applyTo: "**/*"
---

## Command handlers never return an optional event

A command either records what happened or is rejected. It never succeeds without doing anything.

- Do not declare an event return type as nullable (`MyEvent? Handle(...)`), and do not return `null` from `Handle()` to mean "nothing to do". That applies to source, samples, specs, documentation snippets and generated examples.
  - The pipeline treats a null event as success with nothing appended.
  - The caller gets a successful result for a command that never happened, and no event records why.
- When state is missing or a rule does not hold, return `Result<TEvent, ValidationResult>` and `ValidationResult.Error("why", [nameof(Property)])`. Alternatively, reject earlier in a validator or in `Provide()`.
- When the command truly has nothing to record and should still succeed, it is not an event-producing command. Model it as a separate command, a validator rejection, or a no-op the caller does not issue. Do not hide a branch inside one command behind `null`.
- When ARC0006 asks for a nullable read-model parameter, handle the `null` by rejecting, never by returning a nullable event.

Optional *operations* (`ICommandOperation`) are a different contract, documented in Documentation/backend/csharp/commands/operations, and are not covered by this rule.
