// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using HandlebarsDotNet;

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Holds all the available templates.
/// </summary>
public static class TemplateTypes
{
    /// <summary>
    /// The template for a type.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> Type = Handlebars.Compile(GetTemplate("Type"));

    /// <summary>
    /// The template for a type.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> Interface = Handlebars.Compile(GetTemplate("Interface"));

    /// <summary>
    /// The template for a type.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> Enum = Handlebars.Compile(GetTemplate("Enum"));

    /// <summary>
    /// The template for a flags enum — an enum decorated with <see cref="FlagsAttribute"/>.
    /// Includes an <c>allXxx</c> constant that combines all non-zero member values with bitwise OR.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> FlagsEnum = Handlebars.Compile(GetTemplate("FlagsEnum"));

    /// <summary>
    /// The template for a command.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> Command = Handlebars.Compile(GetTemplate("Command"));

    /// <summary>
    /// The template for a query.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> Query = Handlebars.Compile(GetTemplate("Query"));

    /// <summary>
    /// The template for an observable query.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> ObservableQuery = Handlebars.Compile(GetTemplate("ObservableQuery"));

    /// <summary>
    /// The template for the index file of each module.
    /// </summary>
    public static readonly HandlebarsTemplate<object, object> Index = Handlebars.Compile(GetTemplate("index"));

    static TemplateTypes()
    {
        Handlebars.RegisterHelper("camelcase", (writer, _, parameters) => writer.WriteSafeString(parameters[0].ToString()!.ToCamelCase()));
        Handlebars.RegisterHelper("lowercase", (writer, _, parameters) => writer.WriteSafeString(parameters[0].ToString()!.ToLowerInvariant()));
        Handlebars.RegisterHelper("kebabcase", (writer, _, parameters) => writer.WriteSafeString(parameters[0].ToString()!.ToKebabCase()));
        Handlebars.RegisterHelper("ruleargs", (writer, _, parameters) => writer.WriteSafeString(FormatRuleArguments(parameters[0])));
        Handlebars.RegisterHelper("jsstring", (writer, _, parameters) => writer.WriteSafeString(FormatJavaScriptString(parameters[0]?.ToString())));
    }

    /// <summary>
    /// Formats a validation rule's arguments as a TypeScript argument list.
    /// </summary>
    /// <param name="arguments">The rule arguments to format.</param>
    /// <returns>The formatted argument list, empty when there are none.</returns>
    static string FormatRuleArguments(object? arguments) =>
        arguments is IEnumerable<object> values
            ? string.Join(", ", values.Select(FormatRuleArgument))
            : string.Empty;

    /// <summary>
    /// Formats a single rule argument as a TypeScript literal.
    /// </summary>
    /// <param name="value">The argument value.</param>
    /// <returns>The formatted literal.</returns>
    /// <remarks>
    /// A string argument — a <c>matches()</c> pattern in particular — has to be emitted as a quoted, escaped literal.
    /// Writing it bare produces TypeScript that does not parse.
    /// </remarks>
    static string FormatRuleArgument(object? value) => value switch
    {
        null => "null",
        string text => FormatJavaScriptString(text),
        bool flag => flag ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>
    /// Formats a value as a single-quoted, escaped JavaScript string literal.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The quoted literal.</returns>
    static string FormatJavaScriptString(string? value) =>
        $"'{(value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n")}'";

    static string GetTemplate(string name)
    {
        var rootType = typeof(TemplateTypes);
        var stream = rootType.Assembly.GetManifestResourceStream($"{rootType.Namespace}.{name}.hbs");
        if (stream != default)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        return string.Empty;
    }
}
