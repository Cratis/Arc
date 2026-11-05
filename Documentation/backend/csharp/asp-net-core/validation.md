---
title: Cratis Arc MVC input validation
description: Cratis Arc checks MVC model state before controller actions run. Use validation attributes or discovered FluentValidation rules to reject invalid input.
---

Cratis Arc's MVC integration rejects invalid request models before your controller action runs and reports the errors in a command or query result. Use ASP.NET Core validation attributes for rules such as a required field, or FluentValidation when the rule needs conditions or application dependencies. Validation checks input; it does not sanitize or rewrite it.

This page assumes an [ASP.NET Core Arc host](index.md) with MVC registration and controller mapping. Controller request models need not be model-bound `[Command]` types. The account and user validator examples later in the page are alternative fragments: supply their application-owned types and import `FluentValidation`, `Cratis.Concepts`, and the indicated Arc namespace.

## Default behavior

ASP.NET Core binds the request and records binding or validation errors in `ModelState`, its collection of model errors. Arc's `CommandActionFilter` checks that collection before invoking a POST action. Invalid input skips the action and produces HTTP 400 with validation errors in a `CommandResult`. The `QueryActionFilter` also checks model state before invoking a GET action.

`[ApiController]` can return ASP.NET Core's automatic HTTP 400 ProblemDetails response before Arc's filter runs. If you use that attribute, account for that response shape as well as Arc's envelope, the object containing the result and its validation metadata. [Opting out of wrappers](without-wrappers.md) explains the separate choice to return native ASP.NET results.

## Value based validators

Value-based validation uses ASP.NET Core's [validation attributes](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation#custom-attributes).

For example, use `[Required]` from `System.ComponentModel.DataAnnotations` to reject a missing value:

```csharp
public record OpenDebitAccount(
    [Required] AccountId AccountId,
    AccountDetails Details);
```

To see the request and response without the application-owned account types, add this controller and request model to the configured host. It echoes a valid name; it does not create or update an account. The controller deliberately omits `[ApiController]` so an invalid request reaches Arc's model-state check.

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

public class AccountNameRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; init; } = string.Empty;
}

[Route("api/account-names")]
public class AccountNamesController : ControllerBase
{
    [HttpPost]
    public IActionResult Check([FromBody] AccountNameRequest request) =>
        Ok(new { request.Name });
}
```

An empty name fails the required-field rule:

```http
POST /api/account-names HTTP/1.1
Content-Type: application/json

{"name":""}
```

Arc returns HTTP 400 without calling `Check`. These are the relevant response fields with default JSON naming; other fields are omitted:

```json
{
  "isSuccess": false,
  "isValid": false,
  "validationResults": [
    {
      "message": "Name is required",
      "members": ["name"]
    }
  ]
}
```

Send `{"name":"Savings"}` to the same route and the action runs. The HTTP 200 response carries the echoed value inside Arc's `response` field:

```json
{
  "isSuccess": true,
  "isValid": true,
  "validationResults": [],
  "response": { "name": "Savings" }
}
```

This is also a response excerpt. The `members` path in a failed result tells a client which field to mark invalid. For rules that need more than an attribute declaration, use a discovered validator.

## Fluent Validation

[FluentValidation](https://docs.fluentvalidation.net) lets you express rules in a validator class. Arc's MVC registration connects its discoverable validators to ASP.NET Core model validation, so their failures enter the same model state as validation-attribute failures.

### Base Validator

`CommandValidator`, `QueryValidator` and `ConceptValidator` derive from `BaseValidator`. Its concept-aware `RuleFor` overload unwraps a `ConceptAs<>` value, so a rule can validate the underlying string or number without explicitly reading `Value`.

For example, `RuleFor(c => c.Name)` on a concept property returns an `IConceptRuleBuilder<T, TValue>` whose rules run against that primitive. Put a shared rule for the concept in a `ConceptValidator<TConcept>` so MVC can apply it wherever it validates that concept.

`IgnoreConceptRules()` records a property-level exception for Arc's model-graph validation, used by the command pipeline. Call it directly after `RuleFor(...)`, for example `RuleFor(c => c.Name).IgnoreConceptRules().NotEmpty()`. MVC discovers validators for individual model types separately; this is not an opt-out from MVC's concept validation.

### Discoverable validators

Arc discovers validators that implement the marker interface `IDiscoverableValidator<>`. The command, query and concept validator base classes already implement it. For another request-model type, inherit from `DiscoverableValidator<>` to get discovery and the concept-aware rules from `BaseValidator`.

### Command Validator

Derive from `CommandValidator<>` to define rules for a command's properties. For this request model:

```csharp
public record OpenDebitAccount(
    AccountId AccountId,
    AccountDetails Details);
```

The validator rejects a missing name, owner or card choice before the action runs. The fragment assumes `Details` is present; validate that containing object too if your request permits it to be null.

```csharp
using Cratis.Arc.Commands;

public class OpenDebitAccountValidator : CommandValidator<OpenDebitAccount>
{
    public OpenDebitAccountValidator()
    {
        RuleFor(_ => _.Details.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(_ => _.Details.Owner).NotNull().WithMessage("Owner is required");
        RuleFor(_ => _.Details.IncludeCard).NotNull().WithMessage("Include card should be specified");
    }
}
```

MVC model validation runs before Arc establishes a command context. A validator that injects a command-scoped read model therefore cannot run on this path. Use the [model-bound command pipeline](../commands/model-bound/index.md) for that check rather than assuming controller validation has the same scope.

### Concept Validator

A [domain concept](/arc/glossary/) gives a value a specific meaning, such as an account name instead of an arbitrary string. A `ConceptValidator<>` keeps its shared rules beside that type, so each request model does not have to repeat them.

For this concept:

```csharp
public record AccountName(string Value) : ConceptAs<string>(Value);
```

Define a validator for the underlying name:

```csharp
using Cratis.Arc.Validation;

public class AccountNameValidator : ConceptValidator<AccountName>
{
    public AccountNameValidator()
    {
        RuleFor(_ => _).Length(0, 16).WithMessage("Account name has to be less than 16 characters");
    }
}
```

`Length(0, 16)` accepts an empty name and names up to 16 characters long, including exactly 16. Add a required-value rule separately if empty names must fail. MVC skips validators derived from `ConceptValidator<>` on actions or controllers marked `[AspNetResult]`; see [validation without wrappers](without-wrappers.md#validation-still-applies).

### Conditional validation

A concept can appear in both a command and a query without needing identical rules for each. Alongside FluentValidation's [conditions](https://docs.fluentvalidation.net/en/latest/conditions.html), Arc provides `WhenCommand()` and `WhenQuery()` on its validator base classes and rule builders. In MVC validation, Arc marks POST requests as command validation and other HTTP methods as query validation.

For example, a role-based access control system may allow looking up a system user but reject commands that modify one. These fragments assume a `UserId` concept and application-defined rules for looking up users; they do not replace endpoint authorization.

```csharp
using Cratis.Arc.Validation;

public class UserIdValidator : ConceptValidator<UserId>
{
    public UserIdValidator()
    {
        RuleFor(userId => userId)
            .NotNull()
            .UserMustExist().WithMessage("User does not exist.");

        RuleFor(userId => userId)
            .UserMustNotBeSystem().WithMessage("Operation is not allowed on a system user.")
            .WhenCommand();
    }
}
```

`UserMustExist()` and `UserMustNotBeSystem()` are application-owned extension methods, not Arc APIs. The first chain checks that the user exists for both commands and queries. The second applies the system-user restriction only for commands.

By default, `WhenCommand()` conditions all validators in its chain. To combine the chains while keeping the existence check unconditional, use `ApplyConditionTo.CurrentValidator`:

```csharp
using Cratis.Arc.Validation;

public class UserIdValidator : ConceptValidator<UserId>
{
    public UserIdValidator()
    {
        RuleFor(userId => userId)
            .NotNull()
            .UserMustExist().WithMessage("User does not exist.")
            .UserMustNotBeSystem().WithMessage("Operation is not allowed on a system user.")
            .WhenCommand(FluentValidation.ApplyConditionTo.CurrentValidator);
    }
}
```

When several rules share the same condition, group them in the callback form. `UserMustNotBeAdministrator()` below is another application-owned rule:

```csharp
using Cratis.Arc.Validation;

public class UserIdValidator : ConceptValidator<UserId>
{
    public UserIdValidator()
    {
        RuleFor(userId => userId)
            .NotNull()
            .UserMustExist().WithMessage("User does not exist.");

        WhenCommand(() =>
        {
            RuleFor(userId => userId)
                .UserMustNotBeSystem().WithMessage("Operation is not allowed on a system user.")
                .UserMustNotBeAdministrator().WithMessage("Operation is not allowed on an administrator user.");
        });
    }
}
```

Once execution-time validation is in place, [pre-flight command validation](../commands/command-validation.md#controller-based-commands) lets a client check the request through a separate validation route without invoking the controller action.
