---
title: Cratis Arc command operations and recovery
description: Cratis Arc command operations separate decisions from inline side effects, with ordered execution and compensation when commitment permits recovery.
---

Cratis Arc command operations let a model-bound command describe immediate side effects without performing them inside `Handle()`. Arc executes the returned declarations in order and, if the command fails and commitment permits recovery, attempts their optional compensating actions: business work that reverses an earlier attempt.

For a booking command, that means you can inspect which seat it intends to reserve without calling the reservation service. The service call happens later in the pipeline, where Arc tracks attempted work and selects recovery after a failure. You no longer have to build a cleanup stack into the decision.

Command operations are the recommended way to express immediate, inline side effects in model-bound commands. Direct service calls remain supported when the decision needs a service result. Operations require neither Cratis Chronicle nor a separate operation-handler class.

## Decide first, execute afterward

An operation implements `ICommandOperation`. Its public `Execute()` method performs the work, and its optional `Compensate()` method performs the reversal. Arc supplies each method's service parameters from the command's dependency-injection scope.

Your command constructs the operation without invoking either method. Calling `Handle()` directly therefore lets a spec inspect the proposed work without reserving a seat. Calling the command through Arc executes the operation. [CommandScenario](../../testing/command-operations.md) follows the same production pipeline; it does not silently replace execution with a recording stub.

This extends the separation already offered by [Provide()](../../../../scenarios/provide-data-to-a-command.mdx):

```mermaid
flowchart LR
    P[Provide: acquire] --> H[Handle: decide]
    H --> O[Execute operations]
    O --> C[Complete scopes]
    C --> R[Return result]
```

The decision is pure only when it has no I/O, clock reads, randomness, or mutable external state. Returning an operation does not make an otherwise impure handler pure. Supply the identity and state the decision needs rather than creating hidden dependencies inside `Handle()`.

Continue with [implementing an operation](./implementing.md) for a complete reservation example. If you already have a service-backed handler, [migrate its inline work](./migrating.md) while preserving the command's input and caller response.

## You write the reversal, not the error handling

For a reservation, the reversal is canceling the reservation owned by that request. You implement that provider-specific action in `Compensate()`. Arc records which operations started, stops forward execution after a failure, and attempts eligible compensation in reverse order.

That tracking belongs to the pipeline, so the command needs no `try/catch` or rollback stack. If an operation has no useful reversal, omit `Compensate()`. Arc records the missing compensation without claiming to have reversed the work.

Consider four returned operations. A and B complete, C enters `Execute()` and throws, and D never starts. When compensation is permitted, Arc attempts C, then B, then A. It never compensates D.

Why include C? A remote service may have accepted C's request before the connection failed. A thrown exception tells Arc that the local invocation failed. It does not establish whether the external system applied the request.

:::caution[Compensation applies to attempted work]
A compensator must be safe when its operation was partially applied or its acknowledgment was lost. Cancel by an ownership-aware request key. A simple "delete if present" can race a create request still in flight; your provider must supply appropriate terminal cancellation or reconciliation semantics. Duplicate requests must also protect previously finalized work: a new failed attempt must not cancel an earlier successful reservation because they share a key.
:::

The [execution reference](./reference.md) describes ordering, cancellation, and the optional failure context without making those concerns mandatory application boilerplate.

## Compensation is not an atomic rollback

A compensating action is new work. Releasing a reservation does not erase the fact that it was reserved; refunding money does not mean a payment never happened.

With Chronicle, returned events are enrolled before operations execute, and the event transaction completes afterward. A known commit rejection can permit compensation. A successful commit followed by an unrelated cleanup failure must not automatically cancel reservations associated with already-committed business facts.

An unknown commit outcome is different again. A timeout may mean that the store committed but its acknowledgment was lost. Arc does not infer "nothing committed" from a failed `CommandResult`, and it does not automatically retry or reverse operations while commitment is uncertain.

:::note[Recovery stays on the backend]
Arc handles operation failures and compensation on the server. Backend callers and scenarios can inspect recovery observations; the existing HTTP and TypeScript command-result contract stays unchanged. Clients receive the ordinary command success, validation, exception, and response behavior—not operation descriptors or a rollback protocol to implement.
:::

Compensation is best-effort and in-process. It has a separate cancellation token and a shared recovery time budget, but a provider that ignores cancellation can exceed that budget. Operations provide no durable queue or recovery after a process crash. Read the [commit and recovery contract](./reference.md#commit-and-recovery) before relying on reversal.

## Choose the right boundary

Operations run sequentially within a flat command boundary: do not call another command from `Execute()` or `Compensate()`. Any execution scopes that surround the command must explicitly support operations, with at most one scope coordinating a deferred business commit. The [supported scope profile](./reference.md#supported-scope-profile) explains what custom integrations must report.

Choose a different mechanism when the work needs another boundary:

| Your need | Prefer |
| --- | --- |
| Acquire data for a decision | `Provide()` or a directly supplied decision input |
| Reject invalid input or a business decision | A validator or a recognized `ValidationResult` alternative |
| Return information to the caller | An ordinary command response |
| Perform immediate external work chosen by a command | A command operation |
| Reverse an attempted operation when the command fails safely before commitment | Its optional `Compensate()` method |
| Record a durable domain fact with Chronicle | Return an event |
| React reliably to an already-committed fact | A Chronicle reactor or an appropriate durable workflow/outbox |
| Obtain an external call's result to make the decision itself | An explicit service interaction; a post-decision operation cannot supply that earlier input |
| Extend the framework's interpretation of specialized return types | A [response value handler](../response-value-handlers.md) |

Do not replace durable email delivery, payment workflows, or retriable webhooks with inline operations merely to shorten a handler. Choose the delivery and reconciliation guarantees first. [Reactor-returned commands](../../chronicle/reactors/command-side-effects.md) are a separate composition mechanism; a reactor retry can execute its command operations again.

## What to test

Start by calling `Handle()` directly to check the returned operation data and order. Then call the operation's `Execute()` and `Compensate()` methods with a substitute provider to check the requests they make.

Use `CommandScenario` with controlled dependencies to check how those pieces work together: execution order, stopped work after a failure, eligible compensation, and the resulting command outcome.

These checks do not establish whether the production provider handles repeated requests and uncertain cancellations safely. Test that contract at the provider's own integration boundary.

Follow [testing command operations](../../testing/command-operations.md) to turn this separation into executable specifications.
