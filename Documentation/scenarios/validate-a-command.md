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

3. **A rule that depends on existing state → decide inside `Handle()`.** Uniqueness can't be checked against an eventually-consistent read model without a race. Instead, inject the read model Arc resolves for this command's key and return a typed error:

   ```csharp
   public Result<ValidationResult, AuthorRegistered> Handle(RegisteredAuthorName? existing) =>
       existing is not null && existing.Name != AuthorName.NotSet
           ? ValidationResult.Error("An author with that name is already registered.")
           : new AuthorRegistered(Name);
   ```

   The handler isn't the only place a state-dependent rule can live. A `CommandValidator<TCommand>` is resolved through dependency injection, so it can take a collaborator and check state with FluentValidation's `MustAsync` — the rule sits with the rest of the command's rules, and `Handle()` stays focused on producing the event:

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

   A `MustAsync` rule runs on the server only — unlike the declarative rules, it can't be extracted into the generated proxy. And like any pre-handler check, it reads eventually-consistent state: keep the in-`Handle()` guard for invariants that must hold under concurrency, and use the validator when separating rule checking from event production is what you're after.

Validators are discovered by convention — you never register them. The frontend surfaces the messages automatically; see [Execute a command from React](./run-a-command-from-react.md).

## See also

- [Command Validation](/arc/backend/commands/command-validation/) and [Validation](/arc/backend/commands/validation/) — the full validation model.
- [Make it trustworthy](/arc/tutorial/validation/) — the same ideas, taught step by step.
- [Return a result or an error](./return-a-result-or-error.md) — the `Result<,>` return shape used above.
