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
    public const string NameRequiredMessage = "Name is required";
    public const string ValueRangeMessage = "Value must be between 1 and 100";
    public const string EmailFormatMessage = "Invalid email format";

    /// <summary>
    /// Gets or sets the required name.
    /// </summary>
    [Required(ErrorMessage = NameRequiredMessage)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value with range validation.
    /// </summary>
    [Range(1, 100, ErrorMessage = ValueRangeMessage)]
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    [EmailAddress(ErrorMessage = EmailFormatMessage)]
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
    public const string NameRequiredMessage = "Name is required";
    public const string AgeMinimumMessage = "Age must be at least 18";
    public const string EmailRequiredMessage = "Valid email is required";

    public FluentValidatedCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage(NameRequiredMessage);
        RuleFor(c => c.Age).GreaterThanOrEqualTo(18).WithMessage(AgeMinimumMessage);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().WithMessage(EmailRequiredMessage);
    }
}

/// <summary>
/// A command with a response type that has complex nested types.
/// </summary>
[Command]
public class CommandWithComplexResponse
{
    /// <summary>
    /// Gets or sets the input value.
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Handles the command and returns a complex result with nested types.
    /// </summary>
    /// <returns>The command result with nested complex types.</returns>
    public CommandResultWithNestedTypes Handle() => new()
    {
        Message = $"Processed: {Input}",
        Details = new CommandResultDetails
        {
            ProcessedAt = DateTime.UtcNow,
            Metadata = new CommandResultMetadata
            {
                Source = "ProxyGenerator.Specs",
                Version = "1.0.0"
            }
        }
    };
}

/// <summary>
/// Result for CommandWithComplexResponse with nested complex types.
/// </summary>
public class CommandResultWithNestedTypes
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the details which is a complex type.
    /// </summary>
    public CommandResultDetails? Details { get; set; }
}

/// <summary>
/// Details within the command result - a complex type.
/// </summary>
public class CommandResultDetails
{
    /// <summary>
    /// Gets or sets when this was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets metadata which is another complex type.
    /// </summary>
    public CommandResultMetadata? Metadata { get; set; }
}

/// <summary>
/// Metadata within command result details - another level of complex type.
/// </summary>
public class CommandResultMetadata
{
    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// A command with a validator that has constructor dependencies.
/// </summary>
[Command]
public class CommandWithValidatorDependencies
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string PersonId { get; set; } = string.Empty;

    public void Handle()
    {
    }
}

public delegate Task<bool> PersonAlreadyAssignedDelegate(string roleId, string personId);

/// <summary>
/// Validator for CommandWithValidatorDependencies that has constructor dependencies.
/// </summary>
public class CommandWithValidatorDependenciesValidator : CommandValidator<CommandWithValidatorDependencies>
{
    public CommandWithValidatorDependenciesValidator(PersonAlreadyAssignedDelegate personAlreadyAssigned)
    {
        RuleFor(x => x.Name).Length(1, 100).NotEmpty();
        RuleFor(x => x.Age).NotEmpty();
        RuleFor(x => x.RoleId).NotNull();
        RuleFor(x => x.PersonId).NotNull();
        RuleFor(x => x)
            .MustAsync(async (command, ct) => !await personAlreadyAssigned(command.RoleId, command.PersonId))
            .WithMessage("Person is already assigned to this role.");
    }
}
