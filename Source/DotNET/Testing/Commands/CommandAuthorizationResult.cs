// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>An immutable, safe diagnostic from standalone authorization evaluation.</summary>
public sealed class CommandAuthorizationResult
{
    /// <summary>Initializes a new instance of the <see cref="CommandAuthorizationResult"/> class.</summary>
    /// <param name="commandType">The evaluated type.</param>
    /// <param name="outcome">The outcome.</param>
    /// <param name="reasonCode">The stable unsupported reason.</param>
    /// <param name="message">The safe diagnostic.</param>
    internal CommandAuthorizationResult(Type commandType, CommandAuthorizationOutcome outcome, string? reasonCode, string message)
    {
        CommandType = commandType;
        Outcome = outcome;
        ReasonCode = reasonCode;
        Message = message;
    }

    /// <summary>Gets the evaluated command type.</summary>
    public Type CommandType { get; }

    /// <summary>Gets the outcome, including an explicit unsupported category.</summary>
    public CommandAuthorizationOutcome Outcome { get; }

    /// <summary>Gets the stable reason code, when evaluation is unsupported.</summary>
    public string? ReasonCode { get; }

    /// <summary>Gets an explanation that contains no principal or resource contents.</summary>
    public string Message { get; }

    /// <summary>Creates an unsupported diagnostic.</summary>
    /// <param name="type">The command type.</param>
    /// <param name="code">The stable reason code.</param>
    /// <param name="message">The safe explanation.</param>
    /// <returns>The diagnostic.</returns>
    internal static CommandAuthorizationResult Unsupported(Type type, string code, string message) =>
        new(type, CommandAuthorizationOutcome.Unsupported, code, message);

    /// <summary>Creates a runtime verdict.</summary>
    /// <param name="type">The command type.</param>
    /// <param name="authorized">Whether the runtime allowed it.</param>
    /// <returns>The verdict.</returns>
    internal static CommandAuthorizationResult Evaluated(Type type, bool authorized) => new(
        type,
        authorized ? CommandAuthorizationOutcome.Authorized : CommandAuthorizationOutcome.Denied,
        null,
        authorized ? "Runtime authorization allowed the command." : "Runtime authorization denied the command.");
}
