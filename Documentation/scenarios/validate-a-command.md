---
title: Validate a command
description: Reject malformed or duplicate input before a command produces an event — with value-type rules, command rules, and state-dependent business rules.
---

**Goal:** stop bad input from ever becoming an event. A blank name, a negative quantity, a duplicate email — you want the command rejected, with a clear reason, before `Handle()` runs.

## Validation runs before the handler

Arc runs validators *before* it invokes `Handle()`. A command that fails validation never appends anything and returns a `CommandResult` carrying the errors — and because the rules are extracted into the generated proxy, they also run on the client for instant feedback. There are three places a rule can live; reach for the narrowest one that fits.

## Do it

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

3. **A rule that depends on existing state → inject the read model.** Arc resolves the read model for this command's key and hands it to whichever position asks for it — the validator, `Provide()`, or `Handle()`. Where you put the rule depends on whether it must hold under concurrency.

   For an invariant that two simultaneous commands could both slip past — uniqueness is the classic one — guard it in `Handle()`, closest to the append, and return a typed error:

   ```csharp
   public Result<AuthorRegistered, ValidationResult> Handle(RegisteredAuthorName? existing) =>
       existing is not null && existing.Name != AuthorName.NotSet
           ? ValidationResult.Error("An author with that name is already registered.")
           : new AuthorRegistered(Name);
   ```

   Even that guard is a *narrowing*, not a guarantee — for a hard invariant, enforce it with a Chronicle [constraint](/chronicle/constraints/), which is checked at append time.

   Most state-dependent rules aren't races, though. "This order isn't ready to submit", "this account is frozen", "this role doesn't exist" — these are gates on projected state, and they belong in the validator, where they sit with the command's other rules and reach the UI as ordinary validation errors:

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

   The nullable parameter is how you say a missing projection is a business condition rather than a fault — [Use current state in a command](./use-current-state-in-a-command.md) covers that choice and all three positions in full.

   A validator can also reach *outside* the command's own state. It's resolved through dependency injection, so it can take a collaborator and check with FluentValidation's `MustAsync`:

   ```csharp
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
- [Return a result or an error](./return-a-result-or-error.md) — the `Result<,>` return shape used above.
- [Use current state in a command](./use-current-state-in-a-command.md) — injecting projected state into a validator, `Provide()`, or `Handle()`.
