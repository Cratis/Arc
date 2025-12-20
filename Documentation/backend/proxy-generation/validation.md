# Validation

The ProxyGenerator automatically extracts validation rules from your backend FluentValidation validators and generates corresponding TypeScript validation code for the frontend. This ensures that validation rules are defined once on the backend and automatically enforced on both client and server.

## Overview

The validation extraction provides:

- **Automatic Rule Extraction**: Discovers and extracts FluentValidation rules using reflection
- **Type-Safe Generation**: Generates type-safe TypeScript validators for commands and queries
- **Custom Message Preservation**: Extracts and carries over custom error messages to the frontend
- **Version Independence**: Uses reflection-based type checking without hard dependencies on FluentValidation

## How It Works

The ProxyGenerator uses reflection to:

1. **Discover Validators**: Find all `AbstractValidator<T>` implementations for command and query types
2. **Extract Rules**: Analyze validation rules without requiring a FluentValidation package reference
3. **Generate TypeScript**: Create validators with the same rules and messages as the backend
4. **Integrate Automatically**: Generated validators run before server calls

## Supported Validation Rules

The following FluentValidation rules are automatically converted to TypeScript:

| FluentValidation Rule | TypeScript Rule | Generated Code Example |
| --------------------- | --------------- | ---------------------- |
| `NotEmpty()` | `notEmpty()` | `this.ruleFor(c => c.email).notEmpty()` |
| `NotNull()` | `notNull()` | `this.ruleFor(c => c.value).notNull()` |
| `EmailAddress()` | `emailAddress()` | `this.ruleFor(c => c.email).emailAddress()` |
| `MinimumLength(n)` | `minLength(n)` | `this.ruleFor(c => c.name).minLength(2)` |
| `MaximumLength(n)` | `maxLength(n)` | `this.ruleFor(c => c.name).maxLength(50)` |
| `Length(min, max)` | `length(min, max)` | `this.ruleFor(c => c.code).length(3, 10)` |
| `Matches(pattern)` | `matches(pattern)` | `this.ruleFor(c => c.phone).matches(/^\d+$/)` |
| `GreaterThan(n)` | `greaterThan(n)` | `this.ruleFor(c => c.quantity).greaterThan(0)` |
| `GreaterThanOrEqualTo(n)` | `greaterThanOrEqual(n)` | `this.ruleFor(c => c.age).greaterThanOrEqual(18)` |
| `LessThan(n)` | `lessThan(n)` | `this.ruleFor(c => c.discount).lessThan(100)` |
| `LessThanOrEqualTo(n)` | `lessThanOrEqual(n)` | `this.ruleFor(c => c.rating).lessThanOrEqual(5)` |

## Backend Validator Example

Define a validator on the backend using FluentValidation:

```csharp
public class CreateUserCommand
{
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateUserCommandValidator : BaseValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required")
            .EmailAddress();
            
        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18);
            
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(50);
    }
}
```

## Generated TypeScript Validator

The ProxyGenerator automatically generates the corresponding TypeScript validator:

```typescript
export class CreateUserCommandValidator extends CommandValidator<ICreateUserCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.email)
            .notEmpty()
            .withMessage('Email address is required')
            .emailAddress();
        
        this.ruleFor(c => c.age)
            .greaterThanOrEqual(18);
        
        this.ruleFor(c => c.name)
            .notEmpty()
            .minLength(2)
            .maxLength(50);
    }
}
```

## Default Error Messages

All validation rules have sensible default error messages that are automatically used when no custom message is specified:

```typescript
// C# without custom message
RuleFor(x => x.Age).GreaterThanOrEqualTo(18);

// Generated TypeScript (with default message)
this.ruleFor(c => c.age).greaterThanOrEqual(18);
// Default message: "'age' must be greater than or equal to 18."
```

## Custom Error Messages

Custom error messages defined using `.WithMessage()` are automatically extracted and included in the generated validators:

```csharp
// C# with custom message
RuleFor(x => x.Email)
    .NotEmpty()
    .WithMessage("Email address is required");

// Generated TypeScript
this.ruleFor(c => c.email)
    .notEmpty()
    .withMessage('Email address is required');
```

## Version Independence

The ProxyGenerator uses reflection-based type checking instead of hard dependencies on FluentValidation. This prevents version conflicts between the ProxyGenerator tool and your application's FluentValidation version.

### Type Detection Pattern

The generator identifies FluentValidation types using string-based comparisons:

```csharp
// Instead of hard references, uses string matching:
if (baseType.FullName == "FluentValidation.AbstractValidator`1")
{
    // Extract validation rules
}
```

This approach:

- **Eliminates version coupling** between ProxyGenerator and target assemblies
- **Prevents dependency conflicts** when updating FluentValidation
- **Maintains compatibility** across different FluentValidation versions

## Query Validation

Query parameters can also be validated using the same approach:

```csharp
public class SearchUsersQuery
{
    public string SearchTerm { get; set; } = string.Empty;
    public int MinAge { get; set; }
}

public class SearchUsersQueryValidator : BaseValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.SearchTerm).MinimumLength(3);
        RuleFor(x => x.MinAge).GreaterThanOrEqualTo(0).LessThanOrEqualTo(150);
    }
}
```

Generated TypeScript:

```typescript
export class SearchUsersQueryValidator extends QueryValidator<SearchUsersQueryParameters> {
    constructor() {
        super();
        this.ruleFor(c => c.searchTerm).minLength(3);
        this.ruleFor(c => c.minAge).greaterThanOrEqual(0).lessThanOrEqual(150);
    }
}
```

## Limitations

The ProxyGenerator can only extract validation rules that can be executed client-side. The following are **not supported**:

- **Custom validators using `.Must()`**: Business logic that requires server-side execution
- **Async validators**: Rules that make database or service calls
- **Complex predicates**: Conditions that depend on server-side data
- **Cross-property validation**: Rules that compare multiple properties (partially supported)

### Recommended Approach

- Use **out-of-the-box FluentValidation rules** for client-side validation
- Use **`.Must()` and custom validators** for server-side business rules
- Keep **simple validation rules** on the client for better UX
- Always enforce **all validation on the server** for security

## Client-Side Validation Flow

1. User fills out a form and submits a command or query
2. Generated validator runs automatically before the HTTP call
3. If validation fails, errors are returned immediately without server call
4. If validation passes, the request proceeds to the server
5. Server runs the same validation plus any server-only rules

## Best Practices

1. **Define Rules Once**: Always define validation on the backend and let ProxyGenerator extract them
2. **Use Simple Rules**: Keep client-side rules simple and use server-side for complex logic
3. **Custom Messages**: Provide user-friendly messages using `.WithMessage()`
4. **Don't Duplicate**: Never manually write frontend validators - let the generator handle it
5. **Server Authority**: Always validate on the server regardless of client validation

## Related Topics

- [Backend Command Validation](../commands/validation.md) - Defining validation rules on the backend
- [Frontend Validation](../../frontend/core/validation/index.md) - How client-side validation works
- [ProxyGenerator Configuration](./configuration.md) - Configuring the ProxyGenerator
