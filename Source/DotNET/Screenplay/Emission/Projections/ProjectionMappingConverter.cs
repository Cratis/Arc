// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Projections;

/// <summary>
/// Converts the property map of a projection into Screenplay mapping lines.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <param name="location">Where the projection lives, for use in diagnostics.</param>
/// <param name="readModelName">The name of the read model the mappings fill in, for use in diagnostics.</param>
/// <param name="names">The <see cref="NameAvailability"/> deciding which properties a block can map onto.</param>
public class ProjectionMappingConverter(
    IScreenplayNaming naming,
    ScreenplayDiagnostics diagnostics,
    string location,
    string readModelName,
    NameAvailability names)
{
    /// <summary>
    /// The expression incrementing a read model property.
    /// </summary>
    public const string Increment = "$increment";

    /// <summary>
    /// The expression decrementing a read model property.
    /// </summary>
    public const string Decrement = "$decrement";

    /// <summary>
    /// The expression counting occurrences into a read model property.
    /// </summary>
    public const string Count = "$count";

    /// <summary>
    /// The prefix of the expression adding an event property to a read model property.
    /// </summary>
    public const string AddPrefix = "$add(";

    /// <summary>
    /// The prefix of the expression subtracting an event property from a read model property.
    /// </summary>
    public const string SubtractPrefix = "$subtract(";

    /// <summary>
    /// Converts a property map into mapping lines, reporting everything Screenplay cannot express.
    /// </summary>
    /// <param name="properties">The property map of the projection.</param>
    /// <param name="reserved">The <see cref="ReservedWords"/> of the block the mappings are written in.</param>
    /// <returns>The mapping lines, ordered by property.</returns>
    /// <remarks>
    /// Only a plain assignment leads with the property it fills in - <c>increment</c>, <c>count</c>, <c>add</c> and
    /// their kind lead with the operation - so a name the block reserves is only a problem for the former.
    /// </remarks>
    public IEnumerable<MappingSyntax> Convert(IReadOnlyDictionary<string, string> properties, ReservedWords reserved)
    {
        var mappings = new List<MappingSyntax>();

        foreach (var (rawProperty, expression) in properties.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var property = naming.ToPropertyName(rawProperty);
            var mapping = Convert(property, expression);
            if (mapping is null)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableProjectionExpression,
                    $"The expression '{expression}' mapped onto '{rawProperty}' has no counterpart in the projection definition language",
                    location);
                continue;
            }

            if (mapping is SetMappingSyntax && !names.Allows(rawProperty, reserved, readModelName, location))
            {
                continue;
            }

            mappings.Add(mapping);
        }

        return mappings;
    }

    /// <summary>
    /// Converts a single property expression into a mapping line.
    /// </summary>
    /// <param name="property">The camel cased read model property.</param>
    /// <param name="expression">The property expression.</param>
    /// <returns>The mapping line, or <see langword="null"/> when it cannot be expressed.</returns>
    static MappingSyntax? Convert(string property, string expression)
    {
        if (expression == Increment)
        {
            return new IncrementMappingSyntax(property, SourceLocation.Start);
        }

        if (expression == Decrement)
        {
            return new DecrementMappingSyntax(property, SourceLocation.Start);
        }

        if (expression == Count)
        {
            return new CountMappingSyntax(property, SourceLocation.Start);
        }

        if (expression.StartsWith(AddPrefix, StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            return ProjectionExpressionConverter.TryConvert(expression[AddPrefix.Length..^1], out var added)
                ? new AddMappingSyntax(property, added, SourceLocation.Start)
                : null;
        }

        if (expression.StartsWith(SubtractPrefix, StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            return ProjectionExpressionConverter.TryConvert(expression[SubtractPrefix.Length..^1], out var subtracted)
                ? new SubtractMappingSyntax(property, subtracted, SourceLocation.Start)
                : null;
        }

        return ProjectionExpressionConverter.TryConvert(expression, out var source)
            ? new SetMappingSyntax(property, source, SourceLocation.Start)
            : null;
    }
}
