---
title: Rules and fluent API
description: Define frontend validation rules and see which FluentValidation rules Arc maps into TypeScript.
---

Use the fluent API to define client-side checks directly, or let proxy generation emit supported rules from backend validators. The rule chains use familiar FluentValidation patterns. For generated rules, check the supported mappings below rather than assuming every backend condition can run in the client.

## Programmatic rule definition

Use `ruleFor()` to select a member, then chain its checks. This class fragment assumes your application already defines `ICreateUserCommand` and imports `CommandValidator` from `@cratis/arc/commands`:

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

Declaring this class does not attach it to a command. A handwritten command must expose its validator through its configured `validation` member for execution to use it. Generated commands attach their generated validator when proxy generation extracts rules; you do not need to maintain a second copy of those same checks by hand.

## Built-in rules

- `notEmpty()`, `notNull()`
- `minLength()`, `maxLength()`, `length()`
- `emailAddress()`
- `matches(regex)`
- `greaterThan()`, `greaterThanOrEqual()`, `lessThan()`, `lessThanOrEqual()`

For strings, `notEmpty()` rejects empty text and text made entirely of Unicode `White_Space` characters, matching .NET `char.IsWhiteSpace`. This includes U+0085 (NEXT LINE), but not U+FEFF (BYTE ORDER MARK).

## Custom messages

Use `withMessage()` to override defaults. This fragment belongs inside a validator's rule-building code, such as the constructor above:

```typescript
this.ruleFor(c => c.email)
    .notEmpty()
    .withMessage('Email address is required');
```

## Supported FluentValidation mappings

When extracting backend validators, proxy generation translates these rules into their client-side counterparts. Backend rules outside the supported projection still need to run on the server.

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

Complex custom predicates such as `.Must()` are not supported for generated client-side execution.
