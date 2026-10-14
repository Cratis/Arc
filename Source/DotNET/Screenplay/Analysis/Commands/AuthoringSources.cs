// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Holds proven command-local generated values and read dependencies.</summary>
public class AuthoringSources
{
    readonly Dictionary<ISymbol, UnavailableGeneratedValueModel> _unavailable = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ISymbol, string> _paths = new(SymbolEqualityComparer.Default);
    readonly HashSet<ISymbol> _decisionReads = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ExpressionSyntax, string> _expressions = new();

    /// <summary>
    /// Gets whether any generated input is unavailable in this command.
    /// </summary>
    public bool HasUnavailable => _unavailable.Count > 0;

    /// <summary>Registers a directly returned generated expression.</summary>
    /// <param name="expression">The generated expression.</param>
    /// <param name="name">Its declaration name.</param>
    public void AddExpression(ExpressionSyntax expression, string name) => _expressions[expression] = name;

    /// <summary>Registers a source proven by command analysis.</summary>
    /// <param name="symbol">The source symbol.</param>
    /// <param name="path">The command property or read alias.</param>
    /// <param name="decisionRead">Whether Instance selects the read model behind a token.</param>
    public void Add(ISymbol symbol, string path, bool decisionRead = false)
    {
        _paths[symbol] = path;
        if (decisionRead)
        {
            _decisionReads.Add(symbol);
        }
    }

    /// <summary>
    /// Registers the symbol identity of a generated input that remains in code.
    /// </summary>
    /// <param name="symbol">The declared parameter.</param>
    /// <param name="input">Its origin and recovery failure.</param>
    public void AddUnavailable(ISymbol symbol, UnavailableGeneratedValueModel input) => _unavailable[symbol] = input;

    /// <summary>
    /// Finds unavailable generated dependencies, including local aliases and returned constructions.
    /// </summary>
    /// <param name="expression">The expression whose dependencies are needed.</param>
    /// <param name="model">The model resolving symbol identities.</param>
    /// <param name="bindings">The call-site arguments of a reached aggregate behavior.</param>
    /// <returns>The first unavailable dependency, or null for independent expressions.</returns>
    public UnavailableGeneratedValueModel? UnavailableIn(SyntaxNode expression, SemanticModel model, ParameterBindings? bindings = null) => UnavailableIn(expression, model, new(SymbolEqualityComparer.Default), bindings);

    /// <summary>Reads a path into a registered source.</summary>
    /// <param name="expression">The expression being mapped.</param>
    /// <param name="model">The expression's semantic model.</param>
    /// <returns>The proven path, or null.</returns>
    public string? ReadPath(ExpressionSyntax expression, SemanticModel model)
    {
        var current = MappingSourceReader.Unwrap(expression);
        if (_expressions.TryGetValue(current, out var generated))
        {
            return generated;
        }

        var members = new List<string>();
        while (current is MemberAccessExpressionSyntax member)
        {
            members.Insert(0, member.Name.Identifier.ValueText);
            current = MappingSourceReader.Unwrap(member.Expression);
        }

        var symbol = model.GetSymbolInfo(current).Symbol;
        if (symbol is null || !_paths.TryGetValue(symbol, out var path))
        {
            return null;
        }

        if (_decisionReads.Contains(symbol))
        {
            if (members.Count == 0 || members[0] != "Instance")
            {
                return null;
            }

            members.RemoveAt(0);
        }

        return string.Join('.', members.Prepend(path));
    }

    UnavailableGeneratedValueModel? UnavailableIn(SyntaxNode expression, SemanticModel model, HashSet<ISymbol> visited, ParameterBindings? bindings = null)
    {
        foreach (var identifier in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            if (bindings?.Resolve(identifier, model) is { } bound && UnavailableIn(bound.Expression, bound.SemanticModel, visited) is { } boundInput)
            {
                return boundInput;
            }
            var symbol = model.GetSymbolInfo(identifier).Symbol;
            if (symbol is null)
            {
                continue;
            }
            if (_unavailable.TryGetValue(symbol, out var input))
            {
                return input;
            }
            if (symbol is not ILocalSymbol local || !visited.Add(symbol))
            {
                continue;
            }

            foreach (var value in LocalValueOrigins.Values(local, model))
            {
                if (UnavailableIn(value, model, visited, bindings) is { } dependency)
                {
                    return dependency;
                }
            }
        }

        return null;
    }
}
