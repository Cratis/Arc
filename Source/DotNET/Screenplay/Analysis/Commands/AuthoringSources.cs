// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Holds proven command-local generated values and read dependencies.</summary>
public class AuthoringSources
{
    readonly Dictionary<ISymbol, string> _paths = new(SymbolEqualityComparer.Default);
    readonly HashSet<ISymbol> _decisionReads = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ExpressionSyntax, string> _expressions = new();

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
}
