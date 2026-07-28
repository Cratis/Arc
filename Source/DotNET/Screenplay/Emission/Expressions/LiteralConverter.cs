// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Expressions;

/// <summary>
/// Converts a constant value into a Screenplay literal.
/// </summary>
/// <remarks>
/// Every number is converted to a <see cref="double"/> first. The printer formats anything else with the current
/// culture, which emits a comma as the decimal separator on most of the world's machines and produces a document
/// that no longer parses.
/// </remarks>
public static class LiteralConverter
{
    /// <summary>
    /// Converts a constant value.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="naming">The <see cref="IScreenplayNaming"/> used for sanitizing string content.</param>
    /// <returns>The <see cref="LiteralExpressionSyntax"/>.</returns>
    public static LiteralExpressionSyntax Convert(object? value, IScreenplayNaming naming) =>
        new(ToPrintableValue(value, naming), SourceLocation.Start);

    /// <summary>
    /// Determines whether a value is numeric and therefore expressible as a Screenplay number.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True when the value is numeric.</returns>
    public static bool IsNumeric(object? value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    /// <summary>
    /// Reduces a value to something the printer can render without losing it.
    /// </summary>
    /// <param name="value">The value to reduce.</param>
    /// <param name="naming">The <see cref="IScreenplayNaming"/> used for sanitizing string content.</param>
    /// <returns>The value to hand to the printer.</returns>
    static object? ToPrintableValue(object? value, IScreenplayNaming naming) => value switch
    {
        null => null,
        bool boolean => boolean,

        // A concept declares its members in lower camel case and a value referring to one is a string matching that
        // form exactly, so the member goes through the same conversion the declaration was written with.
        EnumValue member => naming.ToStringLiteral(naming.ToPropertyName(member.Member)) ?? string.Empty,
        string text => naming.ToStringLiteral(text) ?? string.Empty,
        _ when IsNumeric(value) => System.Convert.ToDouble(value, CultureInfo.InvariantCulture),
        _ => naming.ToStringLiteral(value.ToString()) ?? string.Empty
    };
}
