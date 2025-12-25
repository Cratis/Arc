// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

/// <summary>
/// Controller for testing controller-based commands.
/// </summary>
[ApiController]
[Route("api/controller-commands")]
public class ControllerCommandsController : ControllerBase
{
    /// <summary>
    /// Executes a simple command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result.</returns>
    [HttpPost("simple")]
    public IActionResult ExecuteSimple([FromBody] ControllerSimpleCommand command) => Ok();

    /// <summary>
    /// Executes a command and returns a result.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result.</returns>
    [HttpPost("with-result")]
    public ActionResult<ControllerCommandResult> ExecuteWithResult([FromBody] ControllerCommandWithResult command)
    {
        return Ok(new ControllerCommandResult
        {
            Message = $"Received: {command.Input}",
            Timestamp = DateTime.UtcNow,
            ReceivedRetryDelay = command.RetryDelay
        });
    }

    /// <summary>
    /// Executes a command with route and query parameters.
    /// </summary>
    /// <param name="id">The route parameter.</param>
    /// <param name="filter">The query parameter.</param>
    /// <param name="command">The command body.</param>
    /// <returns>The result.</returns>
    [HttpPost("{id}")]
    public ActionResult<ControllerParameterResult> ExecuteWithParameters(
        [FromRoute] Guid id,
        [FromQuery] string filter,
        [FromBody] ControllerParameterCommand command)
    {
        return Ok(new ControllerParameterResult
        {
            RouteId = id,
            QueryFilter = filter,
            BodyValue = command.Value
        });
    }

    /// <summary>
    /// Executes a command that throws an exception.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result.</returns>
    /// <exception cref="CommandExecutionFailed">The exception that is thrown when the command is configured to throw.</exception>
    [HttpPost("exception")]
    public IActionResult ExecuteException([FromBody] ControllerExceptionCommand command)
    {
        if (command.ShouldThrow)
        {
            throw new CommandExecutionFailed("Controller command exception");
        }

        return Ok();
    }

    /// <summary>
    /// Executes an authorized command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result.</returns>
    [HttpPost("authorized")]
    [Authorize]
    public IActionResult ExecuteAuthorized([FromBody] ControllerAuthorizedCommand command) => Ok();

    internal static int FluentValidatedCallCount;

    /// <summary>
    /// Executes a fluent validated command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result.</returns>
    [HttpPost("fluent-validated")]
    public IActionResult ExecuteFluentValidated([FromBody] ControllerFluentValidatedCommand command)
    {
        FluentValidatedCallCount++;
        return Ok();
    }

    internal static int DataAnnotationsValidatedCallCount;

    /// <summary>
    /// Executes a data annotations validated command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The result.</returns>
    [HttpPost("data-annotations-validated")]
    public IActionResult ExecuteDataAnnotationsValidated([FromBody] ControllerDataAnnotationsValidatedCommand command)
    {
        DataAnnotationsValidatedCallCount++;
        return Ok();
    }
}

/// <summary>
/// A simple controller-based command.
/// </summary>
public class ControllerSimpleCommand
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// A controller-based command with result.
/// </summary>
public class ControllerCommandWithResult
{
    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the retry delay.
    /// </summary>
    public TimeSpan RetryDelay { get; set; }
}

/// <summary>
/// The result from a controller command.
/// </summary>
public class ControllerCommandResult
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the received retry delay.
    /// </summary>
    public TimeSpan ReceivedRetryDelay { get; set; }
}

/// <summary>
/// A controller-based command with parameters.
/// </summary>
public class ControllerParameterCommand
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// The result with parameters.
/// </summary>
public class ControllerParameterResult
{
    /// <summary>
    /// Gets or sets the route ID.
    /// </summary>
    public Guid RouteId { get; set; }

    /// <summary>
    /// Gets or sets the query filter.
    /// </summary>
    public string QueryFilter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the body value.
    /// </summary>
    public string BodyValue { get; set; } = string.Empty;
}

/// <summary>
/// A controller-based command that throws.
/// </summary>
public class ControllerExceptionCommand
{
    /// <summary>
    /// Gets or sets whether to throw.
    /// </summary>
    public bool ShouldThrow { get; set; }
}

/// <summary>
/// A controller-based authorized command.
/// </summary>
public class ControllerAuthorizedCommand
{
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// A controller-based command with fluent validation.
/// </summary>
public class ControllerFluentValidatedCommand
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Validator for ControllerFluentValidatedCommand using CommandValidator.
/// </summary>
public class ControllerFluentValidatedCommandValidator : CommandValidator<ControllerFluentValidatedCommand>
{
    public const string TitleRequiredMessage = "Title is required";
    public const string QuantityMinimumMessage = "Quantity must be greater than 0";
    public const string EmailRequiredMessage = "Valid email is required";

    public ControllerFluentValidatedCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().WithMessage(TitleRequiredMessage);
        RuleFor(c => c.Quantity).GreaterThan(0).WithMessage(QuantityMinimumMessage);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().WithMessage(EmailRequiredMessage);
    }
}

/// <summary>
/// A controller-based command with abstract validator.
/// </summary>
public class ControllerAbstractValidatedCommand
{
    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Validator for ControllerAbstractValidatedCommand using AbstractValidator directly.
/// </summary>
public class ControllerAbstractValidatedCommandValidator : AbstractValidator<ControllerAbstractValidatedCommand>
{
    public ControllerAbstractValidatedCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty().Length(5).WithMessage("Code must be exactly 5 characters");
        RuleFor(c => c.Amount).GreaterThan(0).WithMessage("Amount must be positive");
    }
}

/// <summary>
/// A controller-based command with data annotations validation.
/// </summary>
public class ControllerDataAnnotationsValidatedCommand
{
    public const string NameRequiredMessage = "Name is required";
    public const string NameLengthMessage = "Name must be between 3 and 50 characters";
    public const string AgeRangeMessage = "Age must be between 18 and 100";
    public const string EmailFormatMessage = "Invalid email address";
    public const string PhoneFormatMessage = "Invalid phone number";
    public const string UrlFormatMessage = "Invalid URL";

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    [Required(ErrorMessage = NameRequiredMessage)]
    [StringLength(50, MinimumLength = 3, ErrorMessage = NameLengthMessage)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the age.
    /// </summary>
    [Range(18, 100, ErrorMessage = AgeRangeMessage)]
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    [Required]
    [EmailAddress(ErrorMessage = EmailFormatMessage)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    [Phone(ErrorMessage = PhoneFormatMessage)]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the website.
    /// </summary>
    [Url(ErrorMessage = UrlFormatMessage)]
    public string Website { get; set; } = string.Empty;
}
