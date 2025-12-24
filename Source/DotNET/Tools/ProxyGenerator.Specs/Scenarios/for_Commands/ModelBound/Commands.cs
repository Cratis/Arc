// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

/// <summary>
/// A simple command for testing.
/// </summary>
[Command]
public class SimpleCommand
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle()
    {
        // Simple command with no result
    }
}

/// <summary>
/// A command with validation for testing.
/// </summary>
[Command]
public class ValidatedCommand
{
    /// <summary>
    /// Gets or sets the required name.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value with range validation.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Value must be between 1 and 100")]
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle()
    {
        // Validated command
    }
}

/// <summary>
/// A command that returns a result.
/// </summary>
[Command]
public class CommandWithResult
{
    /// <summary>
    /// Gets or sets the input value.
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command and returns a result.
    /// </summary>
    /// <returns>The command result data.</returns>
    public CommandResultData Handle() => new()
    {
        ProcessedValue = $"Processed: {Input}",
        Timestamp = DateTime.UtcNow
    };
}

/// <summary>
/// The result returned by CommandWithResult.
/// </summary>
public class CommandResultData
{
    /// <summary>
    /// Gets or sets the processed value.
    /// </summary>
    public string ProcessedValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// A command that throws an exception.
/// </summary>
[Command]
public class ExceptionCommand
{
    /// <summary>
    /// Gets or sets whether to throw.
    /// </summary>
    public bool ShouldThrow { get; set; }

    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <exception cref="CommandExecutionFailed">The exception that is thrown when the command is configured to throw.</exception>
    public void Handle()
    {
        if (ShouldThrow)
        {
            throw new CommandExecutionFailed("Intentional test exception");
        }
    }
}

/// <summary>
/// A command that requires authorization.
/// </summary>
[Command]
[Authorize]
public class AuthorizedCommand
{
    /// <summary>
    /// Gets or sets the secure data.
    /// </summary>
    public string SecureData { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle()
    {
        // Authorized command
    }
}

/// <summary>
/// A command with complex nested types.
/// </summary>
[Command]
public class ComplexCommand
{
    /// <summary>
    /// Gets or sets the nested object.
    /// </summary>
    public NestedType? Nested { get; set; }

    /// <summary>
    /// Gets or sets the list of items.
    /// </summary>
    public List<string> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the dictionary.
    /// </summary>
    public Dictionary<string, int> Values { get; set; } = [];

    /// <summary>
    /// Gets or sets the timeout duration.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Handles the command and returns a result.
    /// </summary>
    /// <returns>The complex command result.</returns>
    public ComplexCommandResult Handle() => new()
    {
        ReceivedNested = Nested?.Name,
        ItemCount = Items.Count,
        ValueCount = Values.Count,
        ReceivedTimeout = Timeout
    };
}

/// <summary>
/// Result for ComplexCommand.
/// </summary>
public class ComplexCommandResult
{
    /// <summary>
    /// Gets or sets the received nested name.
    /// </summary>
    public string? ReceivedNested { get; set; }

    /// <summary>
    /// Gets or sets the item count.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets the value count.
    /// </summary>
    public int ValueCount { get; set; }

    /// <summary>
    /// Gets or sets the received timeout.
    /// </summary>
    public TimeSpan ReceivedTimeout { get; set; }
}

/// <summary>
/// A nested type for testing.
/// </summary>
public class NestedType
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the child.
    /// </summary>
    public NestedChild? Child { get; set; }
}

/// <summary>
/// A nested child type for testing.
/// </summary>
public class NestedChild
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public double Value { get; set; }
}

/// <summary>
/// A command with FluentValidation validator for testing.
/// </summary>
[Command]
public class FluentValidatedCommand
{
    internal static Action OnHandle = () => { };

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the age.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle()
    {
        OnHandle();
    }
}

/// <summary>
/// Validator for FluentValidatedCommand using CommandValidator.
/// </summary>
public class FluentValidatedCommandValidator : CommandValidator<FluentValidatedCommand>
{
    public FluentValidatedCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(c => c.Age).GreaterThanOrEqualTo(18).WithMessage("Age must be at least 18");
        RuleFor(c => c.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required");
    }
}

/// <summary>
/// A command with AbstractValidator for testing.
/// </summary>
[Command]
public class AbstractValidatedCommand
{
    internal static Action OnHandle = () => { };

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command.
    /// </summary>
    public void Handle()
    {
        OnHandle();
    }
}

/// <summary>
/// Validator for AbstractValidatedCommand using AbstractValidator directly.
/// </summary>
public class AbstractValidatedCommandValidator : AbstractValidator<AbstractValidatedCommand>
{
    public AbstractValidatedCommandValidator()
    {
        RuleFor(c => c.Username).NotEmpty().MinimumLength(3).WithMessage("Username must be at least 3 characters");
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters");
    }
}
