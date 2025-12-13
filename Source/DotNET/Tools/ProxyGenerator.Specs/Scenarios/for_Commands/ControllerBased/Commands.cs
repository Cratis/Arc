// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
