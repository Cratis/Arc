// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands;

/// <summary>
/// Maps binding failures to member-aware validation without exposing parser details.
/// </summary>
internal static class CommandBodyBindingFailure
{
    /// <summary>
    /// Converts a deserializer failure into a malformed-request result.
    /// </summary>
    /// <param name="correlationId">The request correlation ID.</param>
    /// <param name="exception">The binding failure, or null for an empty body.</param>
    /// <returns>The validation result.</returns>
    internal static CommandResult From(CorrelationId correlationId, Exception? exception)
    {
        var members = JsonPathMembers.From((exception as JsonException)?.Path);
        var message = exception is IArcBindingRefusal refusal
            ? refusal.MessageFor(members.FirstOrDefault() ?? string.Empty)
            : "The request body could not be read or is not valid for this command.";

        return new CommandResult
        {
            CorrelationId = correlationId,
            ValidationResults = [ValidationResult.Error(message, members, reason: ValidationResultReason.MalformedRequest)]
        };
    }
}
