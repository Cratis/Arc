// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Projections;

/// <summary>
/// Converts the projection property expression mini language into Screenplay projection expressions.
/// </summary>
/// <remarks>
/// Only the forms the projection definition language accepts are produced. Host expressions such as
/// <c>$context.</c> or <c>$env.</c> are errors inside a projection body, so anything that cannot be expressed is
/// reported as unconvertible and left out of the document.
/// </remarks>
public static partial class ProjectionExpressionConverter
{
    /// <summary>
    /// The expression yielding the identifier of the event source an event belongs to.
    /// </summary>
    public const string EventSourceId = "$eventSourceId";

    /// <summary>
    /// The prefix of the expression yielding a value from the event context.
    /// </summary>
    public const string EventContextPrefix = "$eventContext(";

    /// <summary>
    /// The expression yielding the identity that caused an event.
    /// </summary>
    public const string CausedBy = "$causedBy";

    /// <summary>
    /// The prefix of the expression yielding a constant value.
    /// </summary>
    public const string ValuePrefix = "$value(";

    /// <summary>
    /// The prefix of the expression yielding a member of an enumeration.
    /// </summary>
    public const string EnumerationPrefix = "$enum(";

    static readonly string[] _eventContextPaths = ["occurred", "sequenceNumber", "correlationId", "eventSourceId"];
    static readonly string[] _causedByProperties = ["subject", "name", "userName"];

    /// <summary>
    /// Converts a property expression into a Screenplay projection expression.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <param name="converted">The converted expression.</param>
    /// <returns>True when the expression has a projection definition language counterpart.</returns>
    public static bool TryConvert(string expression, out ExpressionSyntax converted)
    {
        converted = null!;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        if (expression == EventSourceId)
        {
            converted = new EventSourceIdExpressionSyntax(SourceLocation.Start);
            return true;
        }

        if (expression.StartsWith(EventContextPrefix, StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            var path = expression[EventContextPrefix.Length..^1];
            if (!_eventContextPaths.Contains(path, StringComparer.Ordinal))
            {
                return false;
            }

            converted = new EventContextExpressionSyntax(path, SourceLocation.Start);
            return true;
        }

        if (expression == CausedBy)
        {
            converted = new CausedByExpressionSyntax(null, SourceLocation.Start);
            return true;
        }

        if (expression.StartsWith($"{CausedBy}.", StringComparison.Ordinal))
        {
            var property = expression[(CausedBy.Length + 1)..];
            if (!_causedByProperties.Contains(property, StringComparer.Ordinal))
            {
                return false;
            }

            converted = new CausedByExpressionSyntax(property, SourceLocation.Start);
            return true;
        }

        if (expression.StartsWith(ValuePrefix, StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            converted = ToLiteral(expression[ValuePrefix.Length..^1]);
            return true;
        }

        // A member of an enumeration is a string naming it and nothing else, so it never goes through the guessing
        // that turns a captured constant back into whichever kind it looks like.
        if (expression.StartsWith(EnumerationPrefix, StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            converted = new LiteralExpressionSyntax(expression[EnumerationPrefix.Length..^1], SourceLocation.Start);
            return true;
        }

        if (expression.StartsWith('$') || !PathExpression().IsMatch(expression))
        {
            return false;
        }

        converted = new PathExpressionSyntax(expression, SourceLocation.Start);

        return true;
    }

    /// <summary>
    /// Converts a captured constant into a Screenplay literal.
    /// </summary>
    /// <param name="value">The invariant string representation of the constant.</param>
    /// <returns>The literal expression.</returns>
    static LiteralExpressionSyntax ToLiteral(string value)
    {
        if (string.Equals(value, "null", StringComparison.Ordinal))
        {
            return new(null, SourceLocation.Start);
        }

        if (bool.TryParse(value, out var boolean))
        {
            return new(boolean, SourceLocation.Start);
        }

        // Every number has to be a double - the printer formats anything else with the current culture.
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? new(number, SourceLocation.Start)
            : new(value.Replace('"', '\''), SourceLocation.Start);
    }

    /// <summary>
    /// Gets the pattern the projection definition language accepts for a property path.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"^@?[A-Za-z_]\w*(\.@?[A-Za-z_$]\w*)*$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PathExpression();
}
