// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Collects the property mappings of one block of a fluent projection.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A mapping is written across two calls - one naming the read model property, the next giving it a value - so the
/// property has to be held between them. The kind of mapping is held with it, because what the value means depends
/// on whether the property was being set, added to or subtracted from.
/// </remarks>
public class FluentMappings(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The call setting a read model property to a value.
    /// </summary>
    public const string Set = "Set";

    /// <summary>
    /// The call adding a value to a read model property.
    /// </summary>
    public const string Add = "Add";

    /// <summary>
    /// The call subtracting a value from a read model property.
    /// </summary>
    public const string Subtract = "Subtract";

    readonly Dictionary<string, string> _properties = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the mappings collected so far, keyed by the read model property they fill in.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties => _properties;

    /// <summary>
    /// Begins a mapping, remembering which property is waiting for a value and what will be done with it.
    /// </summary>
    /// <param name="kind">The name of the call beginning the mapping.</param>
    /// <param name="argument">The lambda selecting the read model property.</param>
    /// <returns>What is now pending, or <see langword="null"/> when the property could not be read.</returns>
    public static string? Begin(string kind, ExpressionSyntax? argument) =>
        ProjectionPaths.ReadDeclared(argument) is { } property ? $"{kind}:{property}" : null;

    /// <summary>
    /// Records a mapping that needs no value of its own.
    /// </summary>
    /// <param name="name">The name of the call.</param>
    /// <param name="argument">The lambda selecting the read model property.</param>
    public void Counter(string name, ExpressionSyntax? argument)
    {
        if (ProjectionPaths.ReadDeclared(argument) is not { } property)
        {
            return;
        }

        _properties[property] = name switch
        {
            "Increment" => ProjectionExpressions.Increment,
            "Decrement" => ProjectionExpressions.Decrement,
            _ => ProjectionExpressions.Count
        };
    }

    /// <summary>
    /// Completes a mapping that was waiting for a value.
    /// </summary>
    /// <param name="pending">The property waiting for a value, if there is one.</param>
    /// <param name="source">The value it was given.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <returns>Nothing further is pending.</returns>
    public string? Complete(string? pending, string? source, string location)
    {
        if (pending is null || source is null)
        {
            Report("a value", location);

            return null;
        }

        var separator = pending.IndexOf(':', StringComparison.Ordinal);
        var kind = pending[..separator];
        var property = pending[(separator + 1)..];

        _properties[property] = kind switch
        {
            Add => ProjectionExpressions.Add(source),
            Subtract => ProjectionExpressions.Subtract(source),
            _ => source
        };

        return null;
    }

    /// <summary>
    /// Reports a call that has no counterpart in the projection definition language.
    /// </summary>
    /// <param name="name">The name of the call.</param>
    /// <param name="location">Where the projection lives.</param>
    public void Report(string name, string location) =>
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
            $"'{name}' has no counterpart in the projection definition language and was left out",
            location);
}
