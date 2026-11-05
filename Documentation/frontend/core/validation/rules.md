---
title: Cratis Arc frontend validation rules
description: Cratis Arc uses ruleFor() to define property checks in TypeScript. Choose built-in rules, customize messages, and understand backend mapping limits.
---

Cratis Arc's `ruleFor()` API defines frontend checks for a property. The validator returns results when those checks fail. Use it to understand generated validators or write an application-owned validator; for shared backend rules, prefer [proxy generation](../../../backend/csharp/proxy-generation/validation.md) so you maintain the rule in one place.

## Programmatic rule definition

Call `ruleFor()` with a property accessor, then chain the checks for that property. This class excerpt assumes `CommandValidator` is imported from `@cratis/arc/commands` and your application supplies an `ICreateUserCommand` interface with `email`, `age`, and `name` properties:

```typescript
class CreateUserCommandValidator extends CommandValidator<ICreateUserCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.email).notEmpty().emailAddress();
        this.ruleFor(c => c.age).greaterThanOrEqual(18);
        this.ruleFor(c => c.name).minLength(2).maxLength(50);
    }
}
```

The email chain checks presence and format, the age rule rejects values below 18, and the name chain accepts lengths from 2 through 50. These property rules produce Error results by default. Call `withSeverity()` after a rule to change its severity; command execution then applies the [severity filtering policy](./severity-filtering.md).

Creating this class alone does not attach it to a command. Generated proxies supply their `validation` instance automatically when supported rules are extracted. For an application-owned command, provide the validator through its `validation` property.

## Built-in rules

| Check | Rules |
| --- | --- |
| Presence | `notEmpty()`, `notNull()` |
| String length | `minLength()`, `maxLength()`, `length()` |
| Format | `emailAddress()`, `url()`, `phone()`, `matches(regex)` |
| Numeric comparison | `greaterThan()`, `greaterThanOrEqual()`, `lessThan()`, `lessThanOrEqual()` |

`notNull()` rejects null and undefined. `notEmpty()` also rejects empty arrays and, for strings, empty text or text made entirely of Unicode `White_Space` characters, matching .NET `char.IsWhiteSpace`. This includes U+0085 (NEXT LINE), but not U+FEFF (BYTE ORDER MARK).

## Custom messages

Place `withMessage()` immediately after the rule whose default message you want to replace. This constructor excerpt gives the presence check a message your form can display:

```typescript
this.ruleFor(c => c.email)
    .notEmpty()
    .withMessage('Email address is required');
```

`withMessage()` applies to the last added property rule, not the whole chain. For example, an `emailAddress()` rule added after this excerpt keeps its own message.

## Supported FluentValidation mappings

The generator translates these supported FluentValidation rules into client rule calls. Similar syntax does not mean that the browser runs the complete backend validator:

| FluentValidation | TypeScript |
| ---------------- | ---------- |
| `NotEmpty()` | `notEmpty()` |
| `NotNull()` | `notNull()` |
| `EmailAddress()` | `emailAddress()` |
| `MinimumLength(n)` | `minLength(n)` |
| `MaximumLength(n)` | `maxLength(n)` |
| `Length(min, max)` | `length(min, max)` |
| `Matches(regex)` | `matches(regex)` |
| `GreaterThan(n)` | `greaterThan(n)` |
| `GreaterThanOrEqualTo(n)` | `greaterThanOrEqual(n)` |
| `LessThan(n)` | `lessThan(n)` |
| `LessThanOrEqualTo(n)` | `lessThanOrEqual(n)` |

## Note

Custom predicates such as `.Must()` and `.MustAsync()`, conditional rules, and dependency-backed business checks are not translated into client-side execution. Numeric comparison mappings require constant numeric values; date ordering and cross-property comparisons remain on the server. A .NET regular expression must also be meaningful in JavaScript.

Keep those checks enforced on the backend. After changing a shared rule, inspect the generated validator rather than assuming every backend rule appeared there. The [validation extraction reference](../../../backend/csharp/proxy-generation/validation.md) covers mapping details, message factories, and constructor requirements; [command and query integration](./integration.md) explains when the attached validator runs.
