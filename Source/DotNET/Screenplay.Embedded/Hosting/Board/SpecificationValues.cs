// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Turns the values a specification states into what the board shows.
/// </summary>
/// <remarks>
/// A literal is carried as the value it is. Anything else - a path, a context or an environment value, a
/// template - names something resolved while the application runs, so it is carried as written.
/// </remarks>
internal static class SpecificationValues
{
    /// <summary>
    /// Gets the values of a set of property mappings, keyed by property.
    /// </summary>
    /// <param name="mappings">The mappings.</param>
    /// <returns>The values.</returns>
    public static JsonObject Of(IEnumerable<PropertyMappingSyntax>? mappings)
    {
        var values = new JsonObject();
        foreach (var mapping in mappings ?? [])
        {
            values[mapping.Property] = ValueOf(mapping.Source);
        }

        return values;
    }

    /// <summary>
    /// Gets the value of a single expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The value.</returns>
    public static JsonNode? ValueOf(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => Literal(literal.Value),
        _ => JsonValue.Create(TextOf(expression))
    };

    /// <summary>
    /// Gets the mappings as Screenplay writes them - <c>name = "Apollo", pages = 120</c>.
    /// </summary>
    /// <param name="mappings">The mappings.</param>
    /// <returns>The text.</returns>
    public static string TextOf(IEnumerable<PropertyMappingSyntax>? mappings) =>
        string.Join(", ", (mappings ?? []).Select(mapping => $"{mapping.Property} = {TextOf(mapping.Source)}"));

    /// <summary>
    /// Gets an expression as Screenplay writes it.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The text.</returns>
    public static string TextOf(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => LiteralText(literal.Value),
        PathExpressionSyntax path => path.Path,
        ContextExpressionSyntax context => $"$context.{context.Path}",
        EnvironmentExpressionSyntax environment => $"$env.{environment.Name}",
        StringsExpressionSyntax strings => $"$strings.{strings.Key}",
        SourceItemExpressionSyntax item => $"$.{item.Path}",
        EventSourceIdExpressionSyntax => "$eventSourceId",
        EventContextExpressionSyntax context => $"$eventContext.{context.Path}",
        CausedByExpressionSyntax causedBy => causedBy.Property is null ? "$causedBy" : $"$causedBy.{causedBy.Property}",
        TemplateExpressionSyntax template => $"`{string.Concat(template.Parts.Select(TemplatePart))}`",
        RawExpressionSyntax raw => raw.Text,
        ListExpressionSyntax list => $"[{string.Join(", ", list.Items.Select(TextOf))}]",
        ObjectExpressionSyntax @object => $"{{ {string.Join(", ", @object.Members.Select(member => $"{Quoted(member.Name)}: {TextOf(member.Value)}"))} }}",
        _ => expression.ToString()
    };

    static string Quoted(string text) => $"\"{text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    static string TemplatePart(TemplatePartSyntax part) => part switch
    {
        TemplateTextSyntax text => text.Text,
        TemplateInterpolationSyntax interpolation => $"${{{TextOf(interpolation.Expression)}}}",
        _ => string.Empty
    };

    static JsonValue? Literal(object? value) => value switch
    {
        null => null,
        string text => JsonValue.Create(text),
        bool flag => JsonValue.Create(flag),
        ExactNumber number => JsonValue.Create(number.Value),
        int number => JsonValue.Create(number),
        long number => JsonValue.Create(number),
        double number => JsonValue.Create(number),
        decimal number => JsonValue.Create(number),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture))
    };

    static string LiteralText(object? value) => value switch
    {
        null => "null",
        string text => Quoted(text),
        bool flag => flag ? "true" : "false",
        ExactNumber number => number.CanonicalText,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
}
