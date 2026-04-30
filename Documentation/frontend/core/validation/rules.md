# Rules And Fluent API

Arc validation for frontend core provides a fluent API that mirrors common FluentValidation patterns.

## Programmatic Rule Definition

Use `ruleFor()` with a chainable API:

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

## Built-in Rules

- `notEmpty()`, `notNull()`
- `minLength()`, `maxLength()`, `length()`
- `emailAddress()`
- `matches(regex)`
- `greaterThan()`, `greaterThanOrEqual()`, `lessThan()`, `lessThanOrEqual()`

## Custom Messages

Use `withMessage()` to override defaults:

```typescript
this.ruleFor(c => c.email)
    .notEmpty()
    .withMessage('Email address is required');
```

## Supported FluentValidation Mappings

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
