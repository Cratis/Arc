// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Builds the minimal plaintext response stored with an effect receipt. It carries the command type name, the outcome, an
/// event count and, for a refusal, only the category and member names — never a command value, a validation message or a
/// handler response — so it holds no personal data and stays far below the 64 KiB receipt limit.
/// </summary>
static class OccurrenceReceiptResponses
{
    const int MaxBytes = 64 * 1024;
    const int MaxMembers = 20;
    const int MaxMemberLength = 100;

    /// <summary>
    /// Builds the response for a commit that appended events.
    /// </summary>
    /// <param name="commandName">The command type name.</param>
    /// <param name="events">The number of events committed.</param>
    /// <returns>The response.</returns>
    internal static JsonNode Appended(string commandName, int events) => new JsonObject
    {
        ["command"] = commandName,
        ["outcome"] = "Appended",
        ["events"] = events
    };

    /// <summary>
    /// Builds the response for a command that succeeded without appending events.
    /// </summary>
    /// <param name="commandName">The command type name.</param>
    /// <returns>The response.</returns>
    internal static JsonNode NoEvents(string commandName) => new JsonObject
    {
        ["command"] = commandName,
        ["outcome"] = "NoEvents"
    };

    /// <summary>
    /// Builds the response for a terminal refusal.
    /// </summary>
    /// <param name="commandName">The command type name.</param>
    /// <param name="reasons">The categorized reasons.</param>
    /// <returns>The response.</returns>
    internal static JsonNode Refused(string commandName, IEnumerable<string> reasons)
    {
        var response = new JsonObject
        {
            ["command"] = commandName,
            ["outcome"] = "Refused",
            ["reasons"] = new JsonArray([.. reasons.Select(_ => JsonValue.Create(_))])
        };

        return Encoding.UTF8.GetByteCount(response.ToJsonString()) <= MaxBytes ? response : new JsonObject
        {
            ["command"] = commandName,
            ["outcome"] = "Refused"
        };
    }

    /// <summary>
    /// Describes why a command was refused without echoing any value or message.
    /// </summary>
    /// <param name="result">The failed <see cref="CommandResult"/>.</param>
    /// <param name="refusal">The classified <see cref="CommandRefusal"/>.</param>
    /// <returns>The categorized reasons.</returns>
    internal static IEnumerable<string> ReasonsFor(CommandResult result, CommandRefusal refusal)
    {
        var members = result.ValidationResults
            .Where(_ => _.Severity == Validation.ValidationResultSeverity.Error)
            .SelectMany(_ => _.Members)
            .Distinct()
            .Take(MaxMembers)
            .Select(_ => _.Length > MaxMemberLength ? _[..MaxMemberLength] : _)
            .ToArray();
        var constraints = refusal.ConstraintNames.Take(MaxMembers).Select(_ => _.Length > MaxMemberLength ? _[..MaxMemberLength] : _).ToArray();

        return refusal.Kind switch
        {
            CommandRefusalKind.Constraint => [$"Constraint violated: {string.Join(", ", constraints)}"],
            _ => members.Length > 0 ? [$"{refusal.Kind} refused: {string.Join(", ", members)}"] : [$"{refusal.Kind} refused"]
        };
    }
}
