// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads deterministic calendar dates and explicitly offset instants for ESM v26 comparisons.
/// </summary>
internal static class DateComparisonOperands
{
    static readonly HashSet<string> _clockMembers = new(StringComparer.Ordinal) { "Now", "UtcNow", "Today" };

    /// <summary>
    /// Gets the date primitive, unwrapping an optional value type.
    /// </summary>
    /// <param name="type">The operand type.</param>
    /// <returns>The Screenplay primitive or null.</returns>
    internal static ScreenplayPrimitive? Primitive(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var underlying] })
        {
            type = underlying;
        }

        return PrimitiveDate(type?.FindBase(WellKnownTypeNames.ConceptAs)?.TypeArguments[0] ?? type);
    }

    /// <summary>
    /// Determines whether an operand may be absent.
    /// </summary>
    /// <param name="type">The operand type.</param>
    /// <returns>Whether the type is nullable.</returns>
    internal static bool IsOptional(ITypeSymbol? type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } || type?.NullableAnnotation == NullableAnnotation.Annotated;

    /// <summary>
    /// Reads a supported date construction or invariant parse call.
    /// </summary>
    /// <param name="expression">The operand expression.</param>
    /// <param name="model">Its semantic model.</param>
    /// <returns>The canonical text or null.</returns>
    internal static string? Literal(ExpressionSyntax expression, SemanticModel model)
    {
        if (!ValuePreservingConversions.Preserves(expression, model))
        {
            return null;
        }

        expression = Commands.MappingSourceReader.Unwrap(expression);
        if (model.GetTypeInfo(expression).Type?.FindBase(WellKnownTypeNames.ConceptAs) is not null)
        {
            var forwarding = new Commands.UuidConstructorForwarding(new SemanticModels([model.Compilation]));

            return expression is ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var argument], Initializer: null } creation &&
                model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor && forwarding.Read(constructor, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default))
                ? Literal(argument.Expression, model) : null;
        }

        return PrimitiveDate(model.GetTypeInfo(expression).Type) is { } primitive
            ? Constructed(expression, primitive, model) ?? Parsed(expression, primitive, model)
            : null;
    }

    /// <summary>
    /// Explains why an operand is not an admitted date literal.
    /// </summary>
    /// <param name="expression">The operand expression.</param>
    /// <param name="model">Its semantic model.</param>
    /// <param name="propertyAdmitted">Whether a command property is an admitted operand.</param>
    /// <returns>The refusal reason.</returns>
    internal static string Refusal(ExpressionSyntax expression, SemanticModel model, bool propertyAdmitted)
    {
        var primitive = Primitive(model.GetTypeInfo(expression).Type);
        if (expression.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Any(access => _clockMembers.Contains(access.Name.Identifier.Text)))
        {
            return "clock operands such as Now, UtcNow and Today are not admitted";
        }
        if (expression is InvocationExpressionSyntax && primitive == ScreenplayPrimitive.DateTime)
        {
            return "a DateTime literal requires an explicit offset or Z";
        }
        if (expression is InvocationExpressionSyntax or ObjectCreationExpressionSyntax && primitive == ScreenplayPrimitive.Date)
        {
            return "a Date literal must be a valid calendar date written as yyyy-MM-dd";
        }

        return propertyAdmitted ? "the operand is neither a literal date or instant nor a command property" : "the operand is not a literal date or instant";
    }

    /// <summary>
    /// Reports that source comparisons alone cannot prove offset-aware input binding.
    /// </summary>
    /// <param name="property">The compared property.</param>
    /// <param name="location">The command location.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    internal static void ReportInputBinding(string property, string location, ScreenplayDiagnostics diagnostics) =>
        diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandProvisioning, $"Compared DateTime input '{property}' requires an explicit offset or Z before deserialization; source comparison recovery does not verify the application's input binding", location);

    static ScreenplayPrimitive? PrimitiveDate(ITypeSymbol? type) => type?.ToDisplayString() switch
    {
        "System.DateOnly" => ScreenplayPrimitive.Date,
        "System.DateTime" or "System.DateTimeOffset" => ScreenplayPrimitive.DateTime,
        _ => null
    };

    static string? Constructed(ExpressionSyntax expression, ScreenplayPrimitive primitive, SemanticModel model)
    {
        if (primitive != ScreenplayPrimitive.Date || expression is not ObjectCreationExpressionSyntax { ArgumentList.Arguments: [var year, var month, var day], Initializer: null } creation)
        {
            return null;
        }
        if (creation.ArgumentList.Arguments.Any(argument => argument.NameColon is not null))
        {
            return null;
        }

        return CalendarDate(model.GetConstantValue(year.Expression).Value, model.GetConstantValue(month.Expression).Value, model.GetConstantValue(day.Expression).Value);
    }

    static string? CalendarDate(object? year, object? month, object? day) =>
        (year, month, day) is (int y, int m, int d) && IsCalendarDate(y, m, d) ? new DateOnly(y, m, d).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;

    static bool IsCalendarDate(int year, int month, int day) => year is >= 1 and <= 9999 && month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month);

    static string? Parsed(ExpressionSyntax expression, ScreenplayPrimitive primitive, SemanticModel model)
    {
        if (expression is not InvocationExpressionSyntax { ArgumentList.Arguments: [var textArgument, var cultureArgument] } call || !IsInvariantParse(call, primitive, model))
        {
            return null;
        }

        return model.GetConstantValue(textArgument.Expression).Value is string text && IsInvariantCulture(cultureArgument.Expression, model) ? Canonical(text, primitive) : null;
    }

    static bool IsInvariantParse(InvocationExpressionSyntax call, ScreenplayPrimitive primitive, SemanticModel model) =>
        model.GetSymbolInfo(call).Symbol is IMethodSymbol { IsStatic: true, Parameters: [{ Type.SpecialType: SpecialType.System_String }, { Type: { } cultureType }] } method &&
        IsParseOf(method, primitive) && IsFormatProvider(cultureType);

    static bool IsParseOf(IMethodSymbol method, ScreenplayPrimitive primitive) => string.Equals(method.Name, "Parse", StringComparison.Ordinal) && PrimitiveDate(method.ContainingType) == primitive;

    static bool IsFormatProvider(ITypeSymbol type) => string.Equals(type.Name, "IFormatProvider", StringComparison.Ordinal) && string.Equals(type.ContainingNamespace?.Name, "System", StringComparison.Ordinal);

    static bool IsInvariantCulture(ExpressionSyntax expression, SemanticModel model) =>
        model.GetSymbolInfo(expression).Symbol is IPropertySymbol { IsStatic: true } culture &&
        string.Equals(culture.Name, "InvariantCulture", StringComparison.Ordinal) && string.Equals(culture.ContainingType.ToDisplayString(), "System.Globalization.CultureInfo", StringComparison.Ordinal);

    /// <summary>
    /// Canonicalizes per decision 0087, accepting instants through the Screenplay package's own parser.
    /// </summary>
    /// <param name="text">The authored literal.</param>
    /// <param name="primitive">The date primitive.</param>
    /// <returns>The canonical text or null.</returns>
    static string? Canonical(string text, ScreenplayPrimitive primitive) => primitive switch
    {
        ScreenplayPrimitive.Date when DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ScreenplayPrimitive.DateTime when SemanticDateComparisons.TryParseInstant(text, out var instant) => instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        _ => null
    };
}
