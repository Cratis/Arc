// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Projections;

/// <summary>
/// Converts the key expressions of a projection into Screenplay keys.
/// </summary>
/// <remarks>
/// The event source id is the implicit key of every block, so it is never written out.
/// <para>
/// Composite keys appear in two shapes. One carries only parts - <c>$composite(Prop=expr,Prop2=expr2)</c> - while
/// the other leads with a type name - <c>$composite(TypeName, Prop=expr)</c>. Both are accepted; when no type name
/// is carried, the read model's own name is used, since that is what the key identifies.
/// </para>
/// </remarks>
public static class ProjectionKeyConverter
{
    /// <summary>
    /// The prefix of a composite key expression.
    /// </summary>
    public const string CompositePrefix = "$composite(";

    /// <summary>
    /// Converts a key expression into a Screenplay key.
    /// </summary>
    /// <param name="key">The key expression to convert.</param>
    /// <param name="readModelName">The name of the read model, used when a composite key carries no type name.</param>
    /// <returns>The <see cref="KeySyntax"/>, or <see langword="null"/> when the default key applies.</returns>
    public static KeySyntax? Convert(string? key, string readModelName)
    {
        if (string.IsNullOrWhiteSpace(key) || string.Equals(key, ProjectionExpressionConverter.EventSourceId, StringComparison.Ordinal))
        {
            return null;
        }

        if (key.StartsWith(CompositePrefix, StringComparison.Ordinal))
        {
            return ConvertComposite(key, readModelName);
        }

        return ProjectionExpressionConverter.TryConvert(key, out var expression)
            ? new ExpressionKeySyntax(expression, SourceLocation.Start)
            : null;
    }

    /// <summary>
    /// Determines whether a key expression is one the default key applies to rather than one that was lost.
    /// </summary>
    /// <param name="key">The key expression to check.</param>
    /// <returns>True when nothing is lost by emitting no key at all.</returns>
    public static bool IsDefault(string? key) =>
        string.IsNullOrWhiteSpace(key) || string.Equals(key, ProjectionExpressionConverter.EventSourceId, StringComparison.Ordinal);

    /// <summary>
    /// Converts a parent key expression, which the grammar takes as a plain expression.
    /// </summary>
    /// <param name="parentKey">The parent key expression to convert.</param>
    /// <returns>The expression, or <see langword="null"/> when there is none.</returns>
    public static ExpressionSyntax? ConvertParent(string? parentKey)
    {
        if (string.IsNullOrWhiteSpace(parentKey) || parentKey.StartsWith(CompositePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        return ProjectionExpressionConverter.TryConvert(parentKey, out var expression) ? expression : null;
    }

    /// <summary>
    /// Converts a composite key expression.
    /// </summary>
    /// <param name="key">The composite key expression.</param>
    /// <param name="readModelName">The name used when the expression carries no type name.</param>
    /// <returns>The key, or <see langword="null"/> when no part could be converted.</returns>
    /// <remarks>
    /// An empty composite key is a compile error, so a composite whose every part is unconvertible is dropped whole
    /// rather than emitted with a missing part - a key that identifies a read model by fewer properties than it
    /// really does would be worse than no key at all.
    /// </remarks>
    static CompositeKeySyntax? ConvertComposite(string key, string readModelName)
    {
        if (!key.EndsWith(')'))
        {
            return null;
        }

        var segments = key[CompositePrefix.Length..^1]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var type = readModelName;
        var parts = new List<KeyPartSyntax>();

        foreach (var segment in segments)
        {
            var separator = segment.IndexOf('=', StringComparison.Ordinal);
            if (separator < 0)
            {
                type = segment;
                continue;
            }

            var property = segment[..separator].Trim();
            if (property.Length == 0 || !ProjectionExpressionConverter.TryConvert(segment[(separator + 1)..].Trim(), out var expression))
            {
                return null;
            }

            parts.Add(new(property, expression, SourceLocation.Start));
        }

        return parts.Count == 0 || type.Length == 0 ? null : new CompositeKeySyntax(type, parts, SourceLocation.Start);
    }
}
