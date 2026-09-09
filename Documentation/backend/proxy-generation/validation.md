---
title: Validation extraction
description: The supported subset of backend rules emitted into TypeScript proxies.
---

Generated validators provide early feedback without duplicating simple rules by hand. They are a **projection of a supported subset**, not an equivalent execution of arbitrary backend validation. Keep the server authoritative: missing client rules do not mean a request is valid, and a local validation result does not establish server authorization.

## Prerequisite and extraction flow

First define a discoverable [command](../commands/index.md) or [query](../queries/index.md). A DTO and validator alone do not create an endpoint or a proxy. Controller-based DTOs are supported when used by a discovered controller endpoint; model-bound types need their normal endpoint declarations.

The executable loads the compiled model, finds a matching validator, and instantiates it to inspect its rules. It also reads supported DataAnnotations. For command model/DTO properties and model-bound query parameters, extraction uses this per-member precedence:

1. Extractable rules from the explicit FluentValidation validator take precedence.
2. Direct concept-property/parameter rules are additive, with identical rules deduplicated.
3. DataAnnotations contribute only where neither of the above has contributed rules for that member.

The current lookup selects the first matching validator type in the searched assembly. Do not rely on it aggregating several validators for the same model.

Controller-based queries use a separate lookup: the generator selects the first DTO whose properties match **all method parameters**, with the same property count, case-insensitive names, and identical types, preferring DTO names containing `Query` (case-insensitive). This lookup includes all method parameters, not just client-facing ones. It extracts rules from the selected DTO, including its supported annotations and concept-property rules. Direct parameter annotations are extracted only if that DTO contributes **no rules at all** (or no DTO matches), rather than as a per-parameter fallback. For example, one DTO rule on `Name` prevents a direct `[Required]` annotation on another parameter from being projected. Direct concept parameters are not independently projected; matching DTO concept properties can contribute rules through DTO extraction.

## FluentValidation rule reference

| Backend rule              | Emitted TypeScript rule                                 |
| ------------------------- | ------------------------------------------------------- |
| `NotEmpty()`              | `notEmpty()`                                            |
| `NotNull()`               | `notNull()`                                             |
| `EmailAddress()`          | `emailAddress()`                                        |
| `MinimumLength(n)`        | `minLength(n)`                                          |
| `MaximumLength(n)`        | `maxLength(n)`                                          |
| `Length(min, max)`        | `length(min, max)`                                      |
| `Length(n)`               | `length(n, n)`                                          |
| `Matches(pattern)`        | `matches(pattern)` with a JavaScript regular expression |
| `GreaterThan(n)`          | `greaterThan(n)`                                        |
| `GreaterThanOrEqualTo(n)` | `greaterThanOrEqual(n)`                                 |
| `LessThan(n)`             | `lessThan(n)`                                           |
| `LessThanOrEqualTo(n)`    | `lessThanOrEqual(n)`                                    |

Comparisons require **numeric constant** values. Cross-property comparisons and nonnumeric constants, such as date sentinels, are not projected. A .NET regex pattern must also be valid and meaningful in JavaScript; extraction is not a regex-language translation.

## A complete command declaration

This C# file can be added to an existing Arc backend with FluentValidation available. It is a minimal response-bearing command, not a persistence example:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace MyApp.Contacts;

public record ContactName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ContactName NotSet = new(string.Empty);
}

public record ContactAge(int Value) : ConceptAs<int>(Value)
{
    public static readonly ContactAge NotSet = new(0);
}

public record EmailAddress(string Value) : ConceptAs<string>(Value)
{
    public static readonly EmailAddress NotSet = new(string.Empty);
}

public class EmailAddressValidator : ConceptValidator<EmailAddress>
{
    public EmailAddressValidator() => RuleFor(email => email.Value)
        .NotEmpty().WithMessage("Email address is required")
        .EmailAddress();
}

[Command]
public record RegisterContact(ContactName Name, EmailAddress Email, ContactAge Age)
{
    public string Handle() => Name.Value.Trim();
}

public class RegisterContactValidator : CommandValidator<RegisterContact>
{
    public RegisterContactValidator()
    {
        RuleFor(contact => contact.Name).NotEmpty().MinimumLength(2).MaximumLength(50);
        RuleFor(contact => contact.Age).GreaterThanOrEqualTo(18);
    }
}
```

`EmailAddressValidator` owns the email invariant wherever that concept appears; the command validator owns this registration's name-length and minimum-age rules. Arc's `CommandValidator` inherits a concept-aware `RuleFor` overload that unwraps `ContactName` and `ContactAge`, so these FluentValidation chains operate on `string` and `int`. Keep each concept in its own file in your application; they are grouped here to make the example self-contained.

Build with the [generator configured](getting-started.md). A successful checkpoint is a generated `RegisterContact` proxy with an `IRegisterContact` interface and attached `RegisterContactValidator`. The response remains an ordinary string; Chronicle is not involved.

This is an **excerpt from its generated validator constructor**, not a separate file to maintain:

```typescript
this.ruleFor((c) => c.age).greaterThanOrEqual(18);
this.ruleFor((c) => c.email)
    .notEmpty()
    .withMessage('Email address is required');
this.ruleFor((c) => c.email).emailAddress();
this.ruleFor((c) => c.name).notEmpty();
this.ruleFor((c) => c.name).minLength(2);
this.ruleFor((c) => c.name).maxLength(50);
```

Each extracted rule gets its own statement. The command template attaches the validator automatically, so you do not instantiate a second validator in the component.

## DataAnnotations reference

| Attribute                                  | Emitted rule                                                                             |
| ------------------------------------------ | ---------------------------------------------------------------------------------------- |
| `[Required]`                               | `notEmpty()`                                                                             |
| `[EmailAddress]`                           | `emailAddress()`                                                                         |
| `[MinLength(n)]`                           | `minLength(n)`                                                                           |
| `[MaxLength(n)]`                           | `maxLength(n)`                                                                           |
| `[StringLength(max)]`                      | `maxLength(max)`                                                                         |
| `[StringLength(max, MinimumLength = min)]` | `length(min, max)` for positive limits                                                   |
| `[Range(min, max)]`                        | `greaterThanOrEqual(min)` and `lessThanOrEqual(max)`                                     |
| `[RegularExpression(pattern)]`             | `matches(pattern)`                                                                       |
| `[Url]`                                    | `url()`                                                                                  |
| `[Phone]`                                  | `phone()`                                                                                |
| `[CreditCard]`                             | `creditCard()` is emitted, but the current client rule builder does **not** implement it |

:::caution
`[CreditCard]` currently exposes a generator/client gap: the emitted call fails TypeScript checking against Arc's current validation API. Do not treat it as supported client validation or manually repair the generated file. Keep server enforcement and account for this limitation before adopting that annotation in a generated endpoint.
:::

These mappings are not full attribute-semantic parity. For example, `Required.AllowEmptyStrings` is not projected. Use the numeric two-argument `Range` overload for these examples; the extractor does not interpret the type/string overload as typed limits. Literal `ErrorMessage` values are read, but resource-based message resolution is not reproduced.

The following alternative deliberately uses primitive properties to demonstrate annotation extraction at an endpoint boundary. Attributes such as `[Url]` and `[Phone]` target primitive values; do not move them unchanged onto concept objects and assume equivalent server behavior. Prefer named concepts with `ConceptValidator<T>` for normal application-domain contracts, as above.

A complete annotation-focused command declaration:

```csharp
using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands.ModelBound;

namespace MyApp.Contacts;

[Command]
public record CheckContact(
    [property: Required(ErrorMessage = "Name is required")] string Name,
    [property: Url] string Website,
    [property: Phone] string PhoneNumber)
{
    public string Handle() => Name;
}
```

After generation, its validator constructor contains these statements (an **output excerpt**):

```typescript
this.ruleFor((c) => c.name)
    .notEmpty()
    .withMessage('Name is required');
this.ruleFor((c) => c.phoneNumber).phone();
this.ruleFor((c) => c.website).url();
```

`url()` and `phone()` are dedicated client APIs, not the previously documented handwritten regular expressions.

## Messages and deferred factories

A literal `.WithMessage("Email address is required")` can be emitted as `.withMessage('Email address is required')`. A deferred `.WithMessage(contact => ...)` factory is **not evaluated or projected**. The rule can still be extracted, with the client rule's default message.

The generator runs at build time, outside the request's culture, tenant, and ambient state. Evaluating a factory there would freeze the wrong context into the proxy. If exact server-resolved wording is essential, use an appropriate server-only validation rule rather than expecting the client to evaluate a factory.

This distinction matters because a blocking client validation result can suppress the request: the server then has no opportunity to resolve a different message. See [frontend validation](../../frontend/core/validation/index.md) for rule behavior and severity handling.

## Query and concept validation

Model-bound query extraction combines a convention-matched argument model's validator, direct concept-parameter validators, and parameter annotations using the same precedence rules. The generated type is `QueryNameParameters`, and its validator derives from `QueryValidator<QueryNameParameters>`. A standalone `SearchUsersQuery` DTO with a validator does not by itself define a query; connect it through the [query argument-model convention](../queries/validation.md).

One-shot queries validate before `perform()`. Observable queries also validate before `subscribe()`; a rejected subscription delivers an invalid `QueryResult` to its callback without opening a connection.

For a direct `ConceptAs<T>` property on an extracted command/query DTO, or a direct model-bound query parameter, the primitive representation receives extractable rules from its concept validator. Controller query parameters require a matching DTO concept property as described above. Explicit model rules remain additive. Concepts nested inside deeper object graphs are not recursively projected by this mechanism. See [backend validation](../commands/validation.md) for server enforcement.

## Limitations to account for

- `Must`, `MustAsync`, custom validators, and dependency-backed business checks are not translated into browser logic.
- Conditions (`When`/`Unless`), rule sets, cascade behavior, per-rule severity, and other FluentValidation execution semantics are not reproduced by the emitted rule statements. A recognized rule inside a condition can become **unconditional** on the client. Do not assume unsupported semantics are safely skipped; inspect and test generated behavior.
- Nested object/collection validation is not a general object-graph translation. Keep server-only rules in an appropriate server-only form and test boundary values against both client and server.
- Constructors run during extraction. A parameterless constructor is preferred; otherwise reference dependencies are passed as `null` and value dependencies receive default values. If construction or reflection fails, extraction can return no FluentValidation rules. A green build is not proof that those rules were emitted.
- Do not perform I/O or dereference services while constructing rules. Capturing a service for a deferred server predicate is different from calling it in the constructor.

After changing validation, inspect the generated validator and test accepted/rejected values in both layers. Do not manually patch generated files or replace server validation with client checks.
