// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Builds the minimal plaintext response stored with an effect receipt. It carries the command type name, the outcome, an
/// event count and, for a refusal, only the category and member names — never a command value, a validation message or a
/// handler response. Every string and collection is bounded, including JSON escaping, below the 64 KiB receipt limit.
/// </summary>
static class OccurrenceReceiptResponses
{
    const int MaxCommandLength = 256;
    const int MaxReasons = 16;
    const int MaxReasonLength = 256;
    const string Truncated = "[truncated]";
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
        ["command"] = BoundText(commandName, MaxCommandLength),
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
        ["command"] = BoundText(commandName, MaxCommandLength),
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
        return new JsonObject
        {
            ["command"] = BoundText(commandName, MaxCommandLength),
            ["outcome"] = "Refused",
            ["reasons"] = new JsonArray([.. BoundReasons(reasons).Select(_ => JsonValue.Create(_))])
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
            .Take(MaxMembers + 1)
            .ToArray();
        var constraints = refusal.ConstraintNames.Take(MaxMembers + 1).ToArray();

        return BoundReasons(refusal.Kind switch
        {
            CommandRefusalKind.Constraint => [$"Constraint violated: {DescribeNames(constraints)}"],
            _ => members.Length > 0 ? [$"{refusal.Kind} refused: {DescribeNames(members)}"] : [$"{refusal.Kind} refused"]
        });
    }

    static IEnumerable<string> BoundReasons(IEnumerable<string> reasons)
    {
        // At most 16 * 256 UTF-16 code units, each escaping to at most six JSON bytes, plus one marker.
        var bounded = reasons.Take(MaxReasons + 1).ToArray();
        return bounded.Take(MaxReasons).Select(_ => BoundText(_, MaxReasonLength))
            .Concat(bounded.Length > MaxReasons ? [Truncated] : []);
    }

    static string DescribeNames(string[] names) => string.Join(", ", names.Take(MaxMembers)
        .Select(_ => BoundText(_, MaxMemberLength)).Concat(names.Length > MaxMembers ? [Truncated] : []));

    static string BoundText(string value, int length)
    {
        if (value.Length <= length)
        {
            return value;
        }

        var cut = length - Truncated.Length;
        if (char.IsHighSurrogate(value[cut - 1]) && char.IsLowSurrogate(value[cut]))
        {
            cut--;
        }

        return $"{value[..cut]}{Truncated}";
    }
}
