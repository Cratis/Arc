---
title: Validate a command
description: Reject malformed or duplicate input before a standalone command writes, using concept rules, command rules, and state-dependent validation.
---

**Goal:** reject bad input before a command changes state. A blank name, a negative quantity, a duplicate email — you want the command rejected, with a clear reason, before `Handle()` runs.

## Validation runs before the handler

Arc runs validators *before* it invokes `Handle()`. A command that fails validation never invokes `Handle()` and returns a `CommandResult` carrying the errors. The proxy generator extracts a **supported subset** of rules for client feedback; async, custom, and state-dependent rules still require the authoritative server check. There are three places a rule can live; reach for the narrowest one that fits.

## Do it

The validator fragments below assume the [standalone tutorial's domain types and imports](/arc/backend/getting-started/your-first-command/). Choose one `RegisterAuthorValidator` example, not multiple competing validators. State/service examples additionally require the provider or application-owned collaborator stated beside them.

1. **A rule that's true of a value everywhere → validate the value type.** Write a `ConceptValidator<T>` and it applies to every command carrying that concept:

   ```csharp
   public class AuthorNameValidator : ConceptValidator<AuthorName>
   {
       public AuthorNameValidator() =>
           RuleFor(x => x.Value).NotEmpty().WithMessage("An author needs a name.");
   }
   ```

2. **A rule specific to one command → validate the command.** Use a `CommandValidator<TCommand>` (FluentValidation) for cross-field or command-only rules:

   ```csharp
   public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
   {
       public RegisterAuthorValidator() =>
           RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
   }
   ```

   For lightweight cases, data annotations like `[Required]` on the command record work too.

3. **A rule that depends on existing state → use a provider or application service.** For by-key read-model injection, configure [provider ownership and an explicit command key](./use-current-state-in-a-command.md). Otherwise inject a database context, collection, or application collaborator and query explicitly.

   A validation pre-check is not atomic, even when moved into `Handle()`. Back uniqueness with a database unique index; use transactions or atomic conditional updates for other invariants. The [tutorial installs the index](/arc/tutorial/validation/) and explains the remaining duplicate-exception translation. [Typed handler failures](./return-a-result-or-error.md) can express a rejected write without implying that a returned object was persisted.

   With **optional Chronicle integration**, a registered event return can be appended and a Chronicle [constraint](/chronicle/constraints/) can enforce its supported invariant at append time. Neither behavior belongs to standalone Arc.Core.

   When a snapshot check is sufficient, place the rule in the validator. If the condition must still hold when the write commits, enforce it through an atomic write, transaction, or appropriate constraint. Order status, account freezes, and role existence can all change after validation; a validator gives early feedback, not a commit-time guarantee:

   This **domain fragment** assumes a keyed `SubmitOrder`, a provider-owned `OrderReadModel`, and an application `OrderStatus` enum:

   ```csharp
   public class SubmitOrderValidator : CommandValidator<SubmitOrder>
   {
       public SubmitOrderValidator(OrderReadModel? order)
       {
           RuleFor(_ => order).NotNull().WithMessage("Order does not exist.");
           When(_ => order is not null, () =>
               RuleFor(_ => order!.Status)
                   .Equal(OrderStatus.ReadyForSubmission)
                   .WithMessage("Only orders that are ready for submission can be submitted."));
       }
   }
   ```

   The nullable parameter is how you say a missing record is a business condition rather than a fault — [Use current state in a command](./use-current-state-in-a-command.md) covers that choice and all three positions in full.

   A validator can also reach *outside* the command's own state. It's resolved through dependency injection, so it can take a collaborator and check with FluentValidation's `MustAsync`:

   `IAuthorsCatalog` is an application-owned contract, not an Arc interface. Its implementation must be available through DI:

   ```csharp
   public interface IAuthorsCatalog
   {
       Task<bool> IsRegistered(AuthorName name);
   }

   public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
   {
       public RegisterAuthorValidator(IAuthorsCatalog authors)
       {
           RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
           RuleFor(c => c)
               .MustAsync(async (command, ct) => !await authors.IsRegistered(command.Name))
               .WithMessage("An author with that name is already registered.");
       }
   }
   ```

   A `MustAsync` rule runs on the server only — unlike the declarative rules, it can't be extracted into the generated proxy.

Validators are discovered by convention — you never register them. The frontend surfaces the messages automatically; see [Execute a command from React](./run-a-command-from-react.md).

## See also

- [Command Validation](/arc/backend/commands/command-validation/) and [Validation](/arc/backend/commands/validation/) — the full validation model.
- [Make it trustworthy](/arc/tutorial/validation/) — the same ideas, taught step by step.
- [Return a result or an error](./return-a-result-or-error.md) — standalone responses and the `Result<,>` return shape.
- [Use current state in a command](./use-current-state-in-a-command.md) — injecting provider-owned state into a validator, `Provide()`, or `Handle()`.
