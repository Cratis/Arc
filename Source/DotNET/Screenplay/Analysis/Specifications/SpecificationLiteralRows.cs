// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads one abstract primary-constructor scenario and proves all its literal rows before folding.
/// </summary>
/// <param name="models">The semantic models resolving parameters and row constants.</param>
/// <param name="diagnostics">Where nonfolded cases are explained.</param>
/// <param name="held">The cached constructions used by ordinary specification readings.</param>
internal sealed class SpecificationLiteralRows(SemanticModels models, ScreenplayDiagnostics diagnostics, HeldValues? held)
{
    /// <summary>
    /// Reads concrete rows using the same reader as their symbolic base.
    /// </summary>
    /// <param name="types">Every source type in this compilation.</param>
    /// <param name="placement">The slice placement shared with ordinary specifications.</param>
    /// <returns>The row readings, including null for unreadable rows.</returns>
    internal Dictionary<INamedTypeSymbol, SpecificationModel?> Read(IEnumerable<INamedTypeSymbol> types, SpecificationPlacement placement)
    {
        var all = types.ToList();
        var reader = new SpecificationReader(models, diagnostics, held);
        var result = new Dictionary<INamedTypeSymbol, SpecificationModel?>(SymbolEqualityComparer.Default);
        foreach (var shared in all.Where(type => type.IsAbstract && Declaration(type)?.ParameterList?.Parameters.Count > 0))
        {
            var rows = all.Where(type => !type.IsAbstract && type.ContainingType is null &&
                SymbolEqualityComparer.Default.Equals(type.BaseType, shared)).OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal).ToList();
            if (rows.Exists(reader.IsSpecification) && placement.SliceOf(shared) is { } slice &&
                rows.TrueForAll(row => placement.SliceOf(row) == slice))
            {
                Read(shared, rows, slice, result);
            }
        }

        return result;
    }

    static ClassDeclarationSyntax? Declaration(INamedTypeSymbol type) => type.DeclaringSyntaxReferences
        .Select(reference => reference.GetSyntax()).OfType<ClassDeclarationSyntax>().FirstOrDefault();

    static Dictionary<string, object?>? Constants(INamedTypeSymbol row, IReadOnlyList<IParameterSymbol> parameters, SemanticModel model)
    {
        if (Declaration(row)?.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault() is not { } baseCall ||
            baseCall.ArgumentList.Arguments.Count != parameters.Count)
        {
            return null;
        }

        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var index = 0; index < parameters.Count; index++)
        {
            var argument = baseCall.ArgumentList.Arguments[index];
            var name = argument.NameColon?.Name.Identifier.ValueText ?? parameters[index].Name;
            if (!IsLiteral(argument.Expression) || model.GetConstantValue(argument.Expression) is not { HasValue: true } constant ||
                !parameters.Any(parameter => parameter.Name == name) || !values.TryAdd(name, constant.Value))
            {
                return null;
            }
        }

        return values;
    }

    static bool IsLiteral(ExpressionSyntax expression) => expression is LiteralExpressionSyntax ||
        (expression is PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax } unary &&
        unary.Kind() is SyntaxKind.UnaryMinusExpression or SyntaxKind.UnaryPlusExpression);

    static string? RowShapeFailure(INamedTypeSymbol row) => Declaration(row) is { Members.Count: 0, ParameterList.Parameters.Count: 0 } && row.DeclaringSyntaxReferences.Length == 1
        ? null
        : $"row '{row.Name}' is not a body-less parameterless concrete class";

    void Read(INamedTypeSymbol shared, List<INamedTypeSymbol> rows, string slice, Dictionary<INamedTypeSymbol, SpecificationModel?> result)
    {
        var declaration = Declaration(shared)!;
        if (models.For(declaration.SyntaxTree) is not { } model)
        {
            return;
        }

        var parameters = declaration.ParameterList!.Parameters.Select(parameter => model.GetDeclaredSymbol(parameter)).OfType<IParameterSymbol>().ToList();
        var symbolicValues = new SpecificationCaseValues(parameters);
        var symbolic = new SpecificationReader(models, new(), held).Read(shared, SpecificationPlacement.NameOf(shared, slice), symbolicValues);
        symbolicValues.Complete(declaration, model);
        var reason = symbolicValues.Failure ?? (symbolic is null ? "the shared scenario cannot be read symbolically" : null);
        var readings = new List<(string Name, SpecificationModel Model, IReadOnlyList<PropertyMappingModel> Values)>();
        foreach (var row in rows)
        {
            reason ??= RowShapeFailure(row);
            ReadRow(row, parameters, slice, result, readings, symbolic, ref reason);
        }

        if (reason is null && symbolic is not null && readings.Count == rows.Count)
        {
            new SpecificationCaseTable(symbolic, symbolicValues.Targets, readings).Register();
            return;
        }

        foreach (var row in rows)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.SpecificationCasesNotFolded,
                $"The scenario '{row.Name}' stays per row because {reason ?? "not every row can be read"}",
                row.ToDisplayString());
        }
    }

    void ReadRow(
        INamedTypeSymbol row,
        IReadOnlyList<IParameterSymbol> parameters,
        string slice,
        Dictionary<INamedTypeSymbol, SpecificationModel?> result,
        List<(string Name, SpecificationModel Model, IReadOnlyList<PropertyMappingModel> Values)> readings,
        SpecificationModel? symbolic,
        ref string? reason)
    {
        var declaration = Declaration(row);
        var constants = declaration is not null && models.For(declaration.SyntaxTree) is { } model ? Constants(row, parameters, model) : null;
        if (constants is null)
        {
            reason ??= $"row '{row.Name}' does not supply one literal constant for every constructor parameter";
            return;
        }

        var bindings = new SpecificationCaseValues(parameters, constants);
        var concrete = new SpecificationReader(models, diagnostics, held).Read(row, SpecificationPlacement.NameOf(row, slice), bindings);
        result[row] = concrete;
        reason ??= bindings.Failure;
        if (concrete is null)
        {
            reason ??= $"row '{row.Name}' cannot be read with its literal values";
            return;
        }

        var values = bindings.Values;
        if (symbolic is not null && !SpecificationCaseComparison.Matches(symbolic, concrete, values.ToDictionary(value => value.Property, value => value.Source, StringComparer.Ordinal)))
        {
            reason ??= $"row '{row.Name}' has different steps, outcomes or caller from the symbolic scenario with its values substituted";
        }

        readings.Add((row.Name, concrete, values));
    }
}
