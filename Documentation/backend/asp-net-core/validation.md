# Validation

The concept of validation is to make sure all your inputs are in a valid form before hitting your logic.
Validation is different from business rules in the sense that it is focused on user input and sanitizing the values
coming as input. While business rules tend to be stateful based on state of your system to be able to validate
for correctness.

## Default behavior

The default behavior when using the `CommandActionFilter` is to stop the execution of a command coming in if it is invalid.
It will automatically look at the `ModelState` of the currently executing controller action and stop execution if things are
invalid. The result will then be put on the `CommandResult` as validation errors for any consumers to react to.

## Value based validators

The value based validation leverages what is already in the ASP.NET Core pipelines [custom attributes](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-6.0#custom-attributes).

You can leverage this by using attributes such as the `[Required]` attribute:

```csharp
public record OpenDebitAccount(
    [Required] AccountId AccountId,
    AccountDetails Details);
```

Attributes are compile time and just represent metadata, the downside of this is that if you're having context dependent rules or need
to inject localized strings or similar you won't be able to get to it.

## Fluent Validation

Another alternative approach to validation is to use [FluentValidation](https://docs.fluentvalidation.net).
Cratis comes with this all setup and automatically hooks up different types of validators for specific purposes.

### Base Validator

The formalized validator types (`CommandValidator`, `QueryValidator` and `ConceptValidator`) all derive from a
type called `BaseValidator`. This base type provides methods for defining validation rules for known `ConceptAs<>`
primitives and will unwrap the inner `Value` property automatically, providing you a clean way of validating the
concepts inner primitive type without considering the `Value` property.

However, if you're hooking up validators for the actual concept, you need to use the method `RuleForConcept()`
to work directly with the concept. The `IRuilBuilderInitial` type returned would then be for the actual concept and
not its primitive type its encapsulating.

### Discoverable validators

Validators can be automatically discovered. This done through the discovery of anything that implements the marker
interface `IDiscoverableValidator<>`. The formalized types for Commands, Queries and Concepts all implement this.
There is also a base type that can be used, which inherits from `BaseValidator` to give you the additional functionality
it provides. All you need to do is inherit from `DiscoverableValidator<>`.

### Command Validator

To write command validators, all you need to do is implement the `CommandValidator<>` class and create
rules for your properties.

Lets say you have a command as follows:

```csharp
public record OpenDebitAccount(
    AccountId AccountId,
    AccountDetails Details);
```

A validator for this could then be as follows:

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

### Concept Validator

When one is using [domain concepts](../general/index.md), you have the opportunity to create a validator for
the concept that will automatically be used as part of the ASP.NET Core validation pipeline.

The benefit of this approach is that you can reuse validation rules and they will automatically implicitly be hooked
up, leading to not have to remember to explicitly add every rule for reused concepts.

The tradeoff of this is obviously that your rules are scattered around.

Say you have a concept as follows:

```csharp
public record AccountName(string Value) : ConceptAs<string>(Value);
```

By inheriting the `ConceptValidator<>` type you can create rules for the concept:

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

### Conditional validation

FluentValidation supports the concept of [conditions](https://docs.fluentvalidation.net/en/latest/conditions.html) for validation,
in the API you can see `When()` for different levels. In Cratis Arc you'll find that every validator
that is a discoverable validator (Concept, Command...) have methods on the base type that offers convenience conditions for
whether or not the request is a **command** or a **query** called `WhenCommand()` or `WhenQuery()`. With this you can
build rule-sets that are specific the scenario of a command or a query given a specific object you're validating.

The use case for this is most relevant for **concepts** where the concept type is used by both commands and queries.

Following is an example for an RBAC system where you have system users that can't be modified and you want to cross cuttingly
apply the validation for all command operations that work on the `UserId` concept but not for any queries that has the
same type as an argument.

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

> Note: The rules `UserMustExist()`and `UserMustNotBeSystem()` are an example of extension methods that you could implement. The
> implementation is irrelevant for the example.

The code sets up a rule that is general without a condition, then it applies a rule for when it is a **command**.
With the `WhenCommand()` extension you can also specify whether or not you want it to apply for the entire validation chain or
just the current. All validators are default, you can then use `ApplyConditionTo.CurrentValidator` for only the current validator.

With the `ApplyConditionTo.CurrentValidator` you can merge into one rule-set:

```csharp
using Cratis.Arc.Validation;

public class UserIdValidator : ConceptValidator<UserId>
{
    public UserIdValidator()
    {
        RuleFor(userId => userId)
            .NotNull()
            .UserMustExist().WithMessage("User does not exist.");
            .UserMustNotBeSystem().WithMessage("Operation is not allowed on a system user.")
            .WhenCommand(ApplyCondition.CurrentValidator);
    }
}
```

If you have multiple rules that should only apply when it is a **command** or **query** you can use the action method:

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
                .UserMustNotBeAdministrator().WithMessage("Operation is not allowed on an administrator user.")
        });
    }
}
```
