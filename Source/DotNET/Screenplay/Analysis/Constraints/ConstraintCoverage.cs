// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Constraints;

/// <summary>
/// Conservatively catalogs rejection and closing targets independently of emitted constraint declarations.
/// </summary>
internal sealed class ConstraintCoverage
{
    static readonly HashSet<string> _configuration = new(StringComparer.Ordinal)
    {
        "ForEventSequences", "ForEventLog", "WithName", "WithMessage", "WithMode", "IgnoreCasing",
        "ForEventSourceType", "ForEventStreamType", "ForEventStreamId", "WithMessageProvider"
    };

    static readonly HashSet<string> _targetMethods = new(StringComparer.Ordinal) { "Unique", "On", "ClosesStreamOn", "RemovedWith" };

    readonly Dictionary<ITypeSymbol, string> _targeted = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ITypeSymbol, string> _closing = new(SymbolEqualityComparer.Default);
    string? _unknownReason;

    /// <summary>
    /// Reads all source constraints and rejects opaque constraints in referenced assemblies.
    /// </summary>
    /// <param name="definitions">All analyzed type declarations.</param>
    /// <param name="models">The semantic models owning their source.</param>
    /// <returns>The conservative coverage catalog.</returns>
    internal static ConstraintCoverage Read(IEnumerable<INamedTypeSymbol> definitions, SemanticModels models)
    {
        var types = definitions.ToList();
        var coverage = new ConstraintCoverage();
        foreach (var type in types.Where(ConstraintReader.IsConstraint))
        {
            coverage.ReadConstraint(type, models);
        }

        var assemblies = types.Select(_ => _.ContainingAssembly.Identity).ToHashSet();
        var compilations = types.SelectMany(_ => _.DeclaringSyntaxReferences).Select(_ => models.For(_.SyntaxTree)?.Compilation).OfType<Compilation>().Distinct();
        var references = compilations.SelectMany(_ => _.SourceModule.ReferencedAssemblySymbols).Distinct<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var assembly in references.Where(_ => !assemblies.Contains(_.Identity)))
        {
            if (TypesIn(assembly.GlobalNamespace).FirstOrDefault(ConstraintReader.IsConstraint) is { } constraint)
            {
                coverage.Unknown(constraint);
            }
        }

        return coverage;
    }

    /// <summary>
    /// Finds why an ordered returned batch may differ from sequential appends.
    /// </summary>
    /// <param name="events">The produced event types in return order.</param>
    /// <returns>The reason equivalence is unproven, or null.</returns>
    internal string? UnprovenBatch(IReadOnlyList<INamedTypeSymbol> events)
    {
        if (events.Count < 2)
        {
            return null;
        }

        return _unknownReason ?? events.Skip(1).Select(RejectionReason).FirstOrDefault(_ => _ is not null) ??
            events.SkipLast(1).Select(ClosingReason).FirstOrDefault(_ => _ is not null);
    }

    static IEnumerable<INamedTypeSymbol> TypesIn(INamespaceOrTypeSymbol scope) =>
        scope.GetMembers().OfType<INamespaceOrTypeSymbol>().SelectMany(member =>
            member is INamedTypeSymbol type ? TypesIn(type).Prepend(type) : TypesIn(member));

    string? RejectionReason(INamedTypeSymbol type)
    {
        if (ConstraintReader.FromEvent(type).FirstOrDefault() is { } constraint)
        {
            return $"'{type.Name}' can be rejected by constraint '{constraint.Name}' after an earlier event";
        }

        return _targeted.TryGetValue(type, out var name)
            ? $"'{type.Name}' can be rejected by constraint '{name}' after an earlier event" : null;
    }

    string? ClosingReason(INamedTypeSymbol type) =>
        type.HasAttribute(WellKnownTypeNames.ClosesStreamAttribute) || _closing.ContainsKey(type)
            ? $"'{type.Name}' closes the stream before a later event" : null;

    void Unknown(INamedTypeSymbol type) => _unknownReason ??= $"constraint '{type.Name}' cannot be read";

    void ReadConstraint(INamedTypeSymbol type, SemanticModels models)
    {
        var contract = type.FindInterface(WellKnownTypeNames.Constraint)!;
        var member = contract.GetMembers(ConstraintReader.DefineMethod).OfType<IMethodSymbol>().FirstOrDefault();
        var method = member is null ? null : type.FindImplementationForInterfaceMember(member) as IMethodSymbol;
        var declarations = method?.DeclaringSyntaxReferences ?? [];
        if (declarations.Length == 0)
        {
            Unknown(type);
        }

        foreach (var reference in declarations)
        {
            ReadDeclaration(type, reference.GetSyntax(), models);
        }
    }

    void ReadDeclaration(INamedTypeSymbol constraint, SyntaxNode declaration, SemanticModels models)
    {
        if (models.For(declaration.SyntaxTree) is not { } model || declaration is not MethodDeclarationSyntax { Body: not null } and not MethodDeclarationSyntax { ExpressionBody: not null })
        {
            Unknown(constraint);
            return;
        }

        foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            ReadInvocation(constraint, invocation, model);
        }
    }

    void ReadInvocation(INamedTypeSymbol constraint, InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            method.ContainingNamespace.ToDisplayString() != "Cratis.Chronicle.Events.Constraints")
        {
            Unknown(constraint);
            return;
        }

        if (method.Name == "Unique" && !method.IsGenericMethod &&
            !invocation.ArgumentList.Arguments.Any(_ => _.Expression is LambdaExpressionSyntax))
        {
            Unknown(constraint);
            return;
        }

        if (_targetMethods.Contains(method.Name))
        {
            ReadTarget(constraint, method);
            return;
        }

        if (!_configuration.Contains(method.Name))
        {
            Unknown(constraint);
        }
    }

    void ReadTarget(INamedTypeSymbol constraint, IMethodSymbol method)
    {
        if (method.TypeArguments is [var target] && target.TypeKind != TypeKind.Error)
        {
            if (string.Equals(method.Name, "Unique", StringComparison.Ordinal) || string.Equals(method.Name, "On", StringComparison.Ordinal))
            {
                _targeted.TryAdd(target, constraint.Name);
            }
            else if (method.Name == "ClosesStreamOn")
            {
                _closing.TryAdd(target, constraint.Name);
            }

            return;
        }

        // The non-generic Unique delegates its coverage to the On calls inside its builder.
        if (method.Name != "Unique")
        {
            Unknown(constraint);
        }
    }
}
