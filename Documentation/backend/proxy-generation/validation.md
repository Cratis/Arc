# Validation

The ProxyGenerator automatically extracts validation rules from your backend validators and generates corresponding TypeScript validation code for the frontend. This ensures that validation rules are defined once on the backend and automatically enforced on both client and server.

The system supports two validation approaches:

- **FluentValidation**: Class-based validators using the FluentValidation library
- **DataAnnotations**: Attribute-based validation using `System.ComponentModel.DataAnnotations`

## Overview

The validation extraction provides:

- **Automatic Rule Extraction**: Discovers and extracts both FluentValidation rules and DataAnnotations attributes using reflection
- **Type-Safe Generation**: Generates type-safe TypeScript validators for commands and queries
- **Custom Message Preservation**: Extracts and carries over custom error messages to the frontend
- **Multiple Validation Styles**: Support for both FluentValidation class-based and DataAnnotations attribute-based validation
- **Version Independence**: Uses reflection-based type checking without hard dependencies on FluentValidation

## How It Works

The ProxyGenerator uses reflection to:

1. **Discover Validators**: Find all `AbstractValidator<T>` implementations and properties with DataAnnotations attributes for command and query types
2. **Extract Rules**: Analyze validation rules from both FluentValidation and DataAnnotations without requiring package references
3. **Generate TypeScript**: Create validators with the same rules and messages as the backend
4. **Integrate Automatically**: Generated validators run before server calls

## FluentValidation Support

### Supported Validation Rules

The following FluentValidation rules are automatically converted to TypeScript:

| FluentValidation Rule | TypeScript Rule | Generated Code Example |
| --------------------- | --------------- | ---------------------- |
| `NotEmpty()` | `notEmpty()` | `this.ruleFor(c => c.email).notEmpty()` |
| `NotNull()` | `notNull()` | `this.ruleFor(c => c.value).notNull()` |
| `EmailAddress()` | `emailAddress()` | `this.ruleFor(c => c.email).emailAddress()` |
| `MinimumLength(n)` | `minLength(n)` | `this.ruleFor(c => c.name).minLength(2)` |
| `MaximumLength(n)` | `maxLength(n)` | `this.ruleFor(c => c.name).maxLength(50)` |
| `Length(min, max)` | `length(min, max)` | `this.ruleFor(c => c.code).length(3, 10)` |
| `Matches(pattern)` | `matches(pattern)` | `this.ruleFor(c => c.phone).matches(/^\d+$/index.md)` |
| `GreaterThan(n)` | `greaterThan(n)` | `this.ruleFor(c => c.quantity).greaterThan(0)` |
| `GreaterThanOrEqualTo(n)` | `greaterThanOrEqual(n)` | `this.ruleFor(c => c.age).greaterThanOrEqual(18)` |
| `LessThan(n)` | `lessThan(n)` | `this.ruleFor(c => c.discount).lessThan(100)` |
| `LessThanOrEqualTo(n)` | `lessThanOrEqual(n)` | `this.ruleFor(c => c.rating).lessThanOrEqual(5)` |

### Backend Validator Example

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

## DataAnnotations Support

As an alternative to FluentValidation, you can use `System.ComponentModel.DataAnnotations` attributes directly on your command and query properties. The ProxyGenerator will automatically extract these attributes and generate equivalent TypeScript validators.

### Supported DataAnnotations Attributes

The following DataAnnotations attributes are automatically converted to TypeScript:

| DataAnnotations Attribute | TypeScript Rule | Example |
| ------------------------- | --------------- | ------- |
| `[Required]` | `notEmpty()` | `[Required] public string Name { get; set; }` |
| `[EmailAddress]` | `emailAddress()` | `[EmailAddress] public string Email { get; set; }` |
| `[MinLength(n)]` | `minLength(n)` | `[MinLength(2)] public string Code { get; set; }` |
| `[MaxLength(n)]` | `maxLength(n)` | `[MaxLength(50)] public string Title { get; set; }` |
| `[StringLength(max)]` | `maxLength(max)` | `[StringLength(100)] public string Description { get; set; }` |
| `[StringLength(max, MinimumLength=min)]` | `length(min, max)` | `[StringLength(50, MinimumLength=3)]` |
| `[Range(min, max)]` | `greaterThanOrEqual(min).lessThanOrEqual(max)` | `[Range(0, 150)] public int Age { get; set; }` |
| `[RegularExpression(pattern)]` | `matches(pattern)` | `[RegularExpression(@"^\d+$")]` |
| `[Url]` | `matches(urlPattern)` | `[Url] public string Website { get; set; }` |
| `[Phone]` | `matches(phonePattern)` | `[Phone] public string PhoneNumber { get; set; }` |

### DataAnnotations Example

Define validation using attributes on your command or query:

```csharp
public class RegisterUserCommand
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress(ErrorMessage = "Valid email address is required")]
    public string Email { get; set; } = string.Empty;
    
    [Range(18, 150, ErrorMessage = "Age must be between 18 and 150")]
    public int Age { get; set; }
    
    [Url]
    public string Website { get; set; } = string.Empty;
    
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}
```

### Generated TypeScript from DataAnnotations

The ProxyGenerator generates a TypeScript validator from the DataAnnotations attributes:

```typescript
export class RegisterUserCommandValidator extends CommandValidator<IRegisterUserCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name)
            .notEmpty()
            .withMessage('Name is required')
            .length(2, 50)
            .withMessage('Name must be between 2 and 50 characters');
        
        this.ruleFor(c => c.email)
            .notEmpty()
            .emailAddress()
            .withMessage('Valid email address is required');
        
        this.ruleFor(c => c.age)
            .greaterThanOrEqual(18)
            .lessThanOrEqual(150)
            .withMessage('Age must be between 18 and 150');
        
        this.ruleFor(c => c.website)
            .matches(/^https?:\/\/.+/index.md);
        
        this.ruleFor(c => c.phoneNumber)
            .matches(/^\+?[1-9]\d{1,14}$/index.md);
    }
}
```

### Choosing Between FluentValidation and DataAnnotations

Both approaches are fully supported, and you can choose based on your preferences:

**FluentValidation**:
- ✅ More expressive and readable for complex validation logic
- ✅ Better separation of concerns (validation in separate class)
- ✅ More flexible and powerful rule composition
- ✅ Easier to unit test validation logic independently

**DataAnnotations**:
- ✅ More concise for simple validation rules
- ✅ Validation rules are co-located with properties
- ✅ No additional dependencies required (built into .NET)
- ✅ Familiar to developers from ASP.NET MVC/Web API

You can also mix both approaches in the same application - the ProxyGenerator will extract rules from both sources.

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

### Validators with Constructor Dependencies

Validators can have constructor dependencies that are used for server-side validation. The ProxyGenerator handles this automatically:

```csharp
public class AssignPersonnelValidator : CommandValidator<AssignPersonnel>
{
    public AssignPersonnelValidator(PersonnelAlreadyAssigned personnelAlreadyAssigned)
    {
        RuleFor(x => x.Name).Length(1, 100).NotEmpty();
        RuleFor(x => x.Age).NotEmpty();
        RuleFor(x => x.RoleId).NotNull();
        RuleFor(x => x.PersonId).NotNull();
        
        // This rule requires server-side execution and won't be extracted
        RuleFor(x => x)
            .MustAsync(async (command, ct) => !await personnelAlreadyAssigned(command.MissionId, command.PersonId))
            .WithMessage("Personnel is already assigned to this mission.");
    }
}
```

The ProxyGenerator will:
1. Create the validator instance with `null` values for dependencies
2. Extract all simple validation rules (`.Length()`, `.NotEmpty()`, `.NotNull()`)
3. Skip rules that require dependencies (`.MustAsync()` with dependency usage)
4. Generate TypeScript with only the client-side compatible rules

This allows you to keep all validation logic in one place while only the client-compatible rules are extracted.

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
