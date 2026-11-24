// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Projections;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Preserves Chronicle expressions until typed emission can validate their key kinds.</summary>
public static class GraphSelectors
{
    const string EventContextPrefix = "$eventContext(";

    /// <summary>Normalizes a selector without turning an expression into an event property path.</summary>
    /// <param name="value">The Chronicle selector, or null for the event source identity.</param>
    /// <returns>The projection expression, retaining unsupported expressions for diagnostic emission.</returns>
    public static string Normalize(object? value) => value switch
    {
        string expression when expression.StartsWith('$') => NormalizeExpression(expression),
        string path => ProjectionPaths.Convert(path) ?? path,
        _ => ProjectionExpressions.EventSourceId
    };

    static string NormalizeExpression(string expression)
    {
        if (expression.StartsWith(EventContextPrefix, StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            var path = expression[EventContextPrefix.Length..^1];
            return ProjectionExpressions.EventContext(ProjectionPaths.Convert(path) ?? path);
        }

        return expression;
    }
}
