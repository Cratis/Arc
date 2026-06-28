---
title: Test a command
description: Prove a slice works through the real Arc pipeline — validation, authorization, and the handler — with no HTTP server and no database.
---

**Goal:** verify a command does the right thing — succeeds when it should, rejects bad input, refuses an unauthorized caller — without standing up a web server or a database.

## The real pipeline, in-process

`CommandScenario<TCommand>` drives a command through the **same** pipeline production uses: validation filters, authorization filters, and the handler all run, nothing is mocked by default. When the Chronicle testing package is referenced, it adds an in-memory event log automatically, so you can also assert on what was appended.

Reference `Cratis.Testing` (the meta-package) in your spec project.

## Do it

1. **Instantiate the scenario** as a field, then `Execute` the command and assert on the `CommandResult`:

   ```csharp
   public class when_registering_an_author
   {
       readonly CommandScenario<RegisterAuthor> _scenario = new();

       [Fact]
       public async Task should_succeed()
       {
           var result = await _scenario.Execute(new RegisterAuthor(AuthorId.New(), "Ada Lovelace"));
           result.ShouldBeSuccessful();
       }
   }
   ```

2. **Check validation without running the handler** using `Validate`:

   ```csharp
   [Fact]
   public async Task should_reject_a_blank_name()
   {
       var result = await _scenario.Validate(new RegisterAuthor(AuthorId.New(), string.Empty));
       result.ShouldHaveValidationErrors();
   }
   ```

3. **Register stubs or fakes** the handler depends on through `Services`, in the test constructor:

   ```csharp
   public when_registering_an_author() =>
       _scenario.Services.AddSingleton(_someDependency);
   ```

The `CommandResult` assertion helpers — `ShouldBeSuccessful`, `ShouldHaveValidationErrors`, `ShouldHaveValidationErrorFor("…")`, `ShouldNotBeAuthorized`, and more — read like sentences and print the failure reasons when they fail.

## See also

- [Command Scenarios](/arc/backend/testing/command-scenario/) — the full API and every assertion helper.
- [Testing Chronicle commands](/arc/backend/testing/chronicle/) — seed the read model state a command reads (from events or a pinned instance) and assert the events it appended.
- [Testing](/arc/backend/testing/) — the testing packages and the Chronicle in-memory event log.
