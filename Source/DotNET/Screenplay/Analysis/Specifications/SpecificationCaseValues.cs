// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Binds primary-constructor parameters at exact specification value positions.
/// </summary>
/// <param name="parameters">The parameters of the shared scenario.</param>
/// <param name="constants">The row constants, or null for a symbolic reading.</param>
internal sealed class SpecificationCaseValues(IReadOnlyList<IParameterSymbol> parameters, IReadOnlyDictionary<string, object?>? constants = null)
{
    readonly Dictionary<string, ITypeSymbol> _targets = new(StringComparer.Ordinal);
    readonly HashSet<(SyntaxTree Tree, int Start)> _used = [];

    /// <summary>
    /// Gets why a parameter cannot be used in a table.
    /// </summary>
    internal string? Failure { get; private set; }

    /// <summary>
    /// Gets the single target type inferred for each parameter.
    /// </summary>
    internal IReadOnlyList<(string Name, TypeReferenceModel Type)> Targets => parameters
        .Where(parameter => _targets.ContainsKey(parameter.Name))
        .Select(parameter => (parameter.Name, new TypeRegistry().Resolve(_targets[parameter.Name])))
        .ToList();

    /// <summary>
    /// Gets normalized concrete row assignments after all target types have been read.
    /// </summary>
    internal IReadOnlyList<PropertyMappingModel> Values => parameters
        .Where(parameter => constants is not null && _targets.ContainsKey(parameter.Name))
        .Select(parameter => new PropertyMappingModel(parameter.Name, new LiteralSource(
            StatableValues.TryState(_targets[parameter.Name], constants![parameter.Name], out var value) ? value : constants[parameter.Name])))
        .ToList();

    /// <summary>
    /// Reads a direct parameter in a value position, retaining ordinary fixture admission.
    /// </summary>
    /// <param name="expression">The exact value expression.</param>
    /// <param name="model">The semantic model resolving the parameter.</param>
    /// <param name="target">The type the value fills.</param>
    /// <param name="allowsNull">Whether this fixture admits optional null.</param>
    /// <param name="source">The symbolic or concrete source.</param>
    /// <returns>Whether the expression names a parameter.</returns>
    internal bool TryRead(ExpressionSyntax expression, SemanticModel model, ITypeSymbol target, bool allowsNull, out MappingSourceModel? source)
    {
        source = null;
        var value = MappingSourceReader.Unwrap(expression);
        if (value is not IdentifierNameSyntax || model.GetSymbolInfo(value).Symbol is not IParameterSymbol parameter ||
            !parameters.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, parameter)))
        {
            return false;
        }

        _used.Add((value.SyntaxTree, value.SpanStart));
        RegisterTarget(parameter, target);
        if (constants is null)
        {
            source = new PropertyPathSource($"case.{parameter.Name}");
        }
        else if (constants.TryGetValue(parameter.Name, out var constant) &&
                 StatableValues.TryState(target, constant, out var stated) && (stated is not null || allowsNull))
        {
            source = new LiteralSource(stated);
        }
        else
        {
            Failure ??= $"parameter '{parameter.Name}' has a value the target fixture cannot admit";
        }

        return true;
    }

    /// <summary>
    /// Requires every parameter reference to occupy a supported whole value position.
    /// </summary>
    /// <param name="declaration">The shared scenario declaration.</param>
    /// <param name="model">The semantic model resolving its references.</param>
    internal void Complete(ClassDeclarationSyntax declaration, SemanticModel model)
    {
        foreach (var reference in declaration.DescendantNodes().OfType<IdentifierNameSyntax>().Where(reference =>
            model.GetSymbolInfo(reference).Symbol is IParameterSymbol parameter &&
            parameters.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, parameter)) &&
            !_used.Contains((reference.SyntaxTree, reference.SpanStart))))
        {
            Failure ??= $"parameter '{reference.Identifier.ValueText}' is used outside a supported whole value position (callers, clocks, redelivery and examples cannot use case parameters)";
        }

        if (_targets.Count != parameters.Count)
        {
            Failure ??= "not every parameter feeds a single target type";
        }
    }

    void RegisterTarget(IParameterSymbol parameter, ITypeSymbol target)
    {
        if (_targets.TryGetValue(parameter.Name, out var previous) && !SymbolEqualityComparer.IncludeNullability.Equals(previous, target))
        {
            Failure ??= $"parameter '{parameter.Name}' feeds inconsistent target types";
        }
        else
        {
            _targets[parameter.Name] = target;
        }
    }
}
