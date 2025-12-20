# Validation

The Arc provides a comprehensive client-side validation system for TypeScript/JavaScript that mirrors FluentValidation's intuitive API. The validation system allows you to define validation rules programmatically with a fluent interface, and automatically integrates with commands and queries for pre-flight validation before server calls.

## Overview

The validation system provides:

- **Fluent API**: Define validation rules using a chainable, type-safe API similar to FluentValidation
- **Automatic Integration**: Validation runs automatically before command execution and query performance
- **Custom Messages**: Override default error messages with custom ones
- **Extensible**: Built on abstract classes allowing custom rule creation
- **Type Safety**: Full TypeScript support with generic type parameters

## Key Features

### Programmatic Rule Definition

Define validation rules using the `ruleFor()` method with lambda expressions:

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

### Built-in Validation Rules

The system includes common validation rules:

- **Not Empty/Null**: `notEmpty()`, `notNull()`
- **String Length**: `minLength()`, `maxLength()`, `length()`
- **Email**: `emailAddress()`
- **Pattern Matching**: `matches(regex)`
- **Numeric Comparison**: `greaterThan()`, `greaterThanOrEqual()`, `lessThan()`, `lessThanOrEqual()`

### Default Error Messages

All validation rules have sensible default error messages that include the property name:

```typescript
// Default message: "'email' must not be empty."
this.ruleFor(c => c.email).notEmpty();

// Default message: "'age' must be greater than or equal to 18."
this.ruleFor(c => c.age).greaterThanOrEqual(18);
```

### Custom Error Messages

Override default messages using `withMessage()`:

```typescript
this.ruleFor(c => c.email)
    .notEmpty()
    .withMessage('Email address is required');
    
this.ruleFor(c => c.age)
    .greaterThanOrEqual(18)
    .withMessage('You must be at least 18 years old');
```

## Backend-Governed Validation

The validation rules are defined on the backend using FluentValidation and automatically extracted by the ProxyGenerator during the build process. This ensures:

- **Single Source of Truth**: Validation rules are defined once on the backend
- **Consistency**: Frontend and backend validation use the same rules
- **Type Safety**: Generated validators are type-safe and match your command/query types
- **No Duplication**: No need to maintain separate validation logic on frontend and backend

## Usage with Commands

Commands automatically run client-side validation before executing:

```typescript
const command = new CreateUserCommand();
command.email = '';  // Invalid
command.age = 15;    // Invalid

const result = await command.execute();
// result.isValid === false
// result.validationResults contains errors with messages
```

## Usage with Queries

Queries automatically validate parameters before performing the request:

```typescript
const query = new SearchUsersQuery();
query.parameters = { searchTerm: 'ab', minAge: -5 };  // Invalid

const result = await query.perform();
// result.isValid === false
// result.validationResults contains errors
```

## Validation Results

Validation failures return a `CommandResult` or `QueryResult` with validation errors:

```typescript
interface ValidationResult {
    severity: ValidationResultSeverity;
    message: string;
    members: string[];
    state: any;
}
```

Example validation result:

```typescript
{
    severity: ValidationResultSeverity.Error,
    message: "Email address is required",
    members: ["email"],
    state: null
}
```

## Creating Custom Validators

You can create custom validators by extending the `Validator<T>` base class:

```typescript
class CustomValidator extends Validator<MyType> {
    constructor() {
        super();
        this.ruleFor(t => t.property).notEmpty().minLength(5);
        // Add more rules as needed
    }
}

const validator = new CustomValidator();
const results = validator.validate(instance);
const isValid = validator.isValidFor(instance);
```

## How It Works

1. **Backend Definition**: Define validators using FluentValidation on your backend commands/queries
2. **ProxyGeneration**: The ProxyGenerator extracts validation rules during build using reflection
3. **TypeScript Generation**: Validators are generated as TypeScript classes with the same rules
4. **Client Execution**: Validation runs automatically before server calls
5. **Error Handling**: Validation errors are returned in the command/query result

## Supported FluentValidation Rules

The following FluentValidation rules are automatically converted to TypeScript:

| FluentValidation | TypeScript | Description |
| ---------------- | ---------- | ----------- |
| `NotEmpty()` | `notEmpty()` | Value must not be null, undefined, empty string, or empty array |
| `NotNull()` | `notNull()` | Value must not be null or undefined |
| `EmailAddress()` | `emailAddress()` | Value must be a valid email address |
| `MinimumLength(n)` | `minLength(n)` | String must have at least n characters |
| `MaximumLength(n)` | `maxLength(n)` | String must have at most n characters |
| `Length(min, max)` | `length(min, max)` | String must be between min and max characters |
| `Matches(regex)` | `matches(regex)` | String must match the regular expression |
| `GreaterThan(n)` | `greaterThan(n)` | Number must be greater than n |
| `GreaterThanOrEqualTo(n)` | `greaterThanOrEqual(n)` | Number must be greater than or equal to n |
| `LessThan(n)` | `lessThan(n)` | Number must be less than n |
| `LessThanOrEqualTo(n)` | `lessThanOrEqual(n)` | Number must be less than or equal to n |

> **Note**: Custom validators using `.Must()` or complex business rules are not supported, as they require server-side execution.

## Best Practices

1. **Keep Rules Simple**: Only use out-of-the-box validation rules that can be executed client-side
2. **Use Custom Messages**: Provide user-friendly messages for better UX
3. **Backend Authority**: Always define validation rules on the backend for consistency
4. **Don't Duplicate**: Let the ProxyGenerator handle rule extraction rather than manually writing frontend validators
5. **Server Validation**: Remember that client validation is for UX - server validation is always required for security

## Related Topics

- [Commands](../commands.md) - How commands integrate with validation
- [Queries](../queries.md) - How queries integrate with validation
- [Backend Validation](../../../backend/commands/validation.md) - Defining validation rules on the backend
- [Proxy Generation Validation](../../../backend/proxy-generation/validation.md) - How the ProxyGenerator extracts validation rules
