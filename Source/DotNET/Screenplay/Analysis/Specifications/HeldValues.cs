// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Finds the construction a step of a specification states, following a value the specification holds one hop.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every tree is read through.</param>
/// <remarks>
/// A step states what it is about by constructing it, and most write that construction where the step is. Holding it
/// in a member instead is just as common - the same event is stated once and asserted on later, or the command is
/// built where the values it needs are - and the step then names the member rather than the construction. Reading
/// only what is written inline leaves those scenarios out whole, which is the largest single category of scenario a
/// real application loses.
/// <para>
/// One hop, to a single unconditional assignment, and no further. Following a chain would mean reasoning about what
/// a value was at the moment the step ran, which is the discipline everything else here deliberately does not keep:
/// a member assigned in two places, or assigned inside a branch, held different values in different runs, and the
/// source text does not say which one the step saw. Those stay unreadable and the scenario is left out and said so,
/// because a scenario stating a world nobody specified is worse than one honestly missing.
/// </para>
/// <para>
/// The hop routinely lands in another tree, and often in another project's compilation - a base context is written
/// below the scenario inheriting it. So which model reads a tree is asked of <see cref="SemanticModels"/> rather
/// than taken from one compilation, which throws for a tree it does not own.
/// </para>
/// </remarks>
public class HeldValues(SemanticModels models)
{
    readonly Dictionary<Compilation, Dictionary<ISymbol, bool>> _stable = [];
    readonly Dictionary<Compilation, Writes> _writes = [];

    /// <summary>
    /// Gets the construction an expression stands for.
    /// </summary>
    /// <param name="expression">The expression the step states.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>The <see cref="HeldConstruction"/>, or <see langword="null"/> when there is none this reads.</returns>
    public HeldConstruction? ConstructionOf(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var unwrapped = MappingSourceReader.Unwrap(expression);
        if (unwrapped is BaseObjectCreationExpressionSyntax inline)
        {
            return new(inline, semanticModel);
        }

        if (unwrapped is not (IdentifierNameSyntax or MemberAccessExpressionSyntax))
        {
            return null;
        }

        return Held(semanticModel.GetSymbolInfo(unwrapped).Symbol);
    }

    /// <summary>
    /// Determines whether repeated references name a stable held value.
    /// </summary>
    /// <param name="symbol">The member or local to check.</param>
    /// <param name="compilation">The specification compilation containing possible reassignments.</param>
    /// <returns>Whether the value is assigned once without a computed getter.</returns>
    public bool IsStable(ISymbol symbol, Compilation compilation)
    {
        if (!_stable.TryGetValue(compilation, out var symbols))
        {
            _stable[compilation] = symbols = new(SymbolEqualityComparer.Default);
        }

        if (!symbols.TryGetValue(symbol, out var stable))
        {
            symbols[symbol] = stable = ReadStability(symbol, compilation);
        }

        return stable;
    }

    /// <summary>
    /// Gets the single initializer of a stable member, without following another held value.
    /// </summary>
    /// <param name="symbol">The member to read.</param>
    /// <param name="compilation">The compilation containing possible reassignments.</param>
    /// <returns>The initializer, or null when another write or a computed getter prevents reading it.</returns>
    internal ExpressionSyntax? InitializerOf(ISymbol symbol, Compilation compilation)
    {
        if (symbol is not (IFieldSymbol or IPropertySymbol) || !IsStable(symbol, compilation) ||
            GivenTo(symbol).Take(2).ToList() is not [var value] || !Unconditional(value))
        {
            return null;
        }

        return symbol.DeclaringSyntaxReferences.Select(reference => DeclaredValueOf(reference.GetSyntax()))
            .FirstOrDefault(initializer => initializer is not null && initializer.SyntaxTree == value.SyntaxTree && initializer.Span == value.Span);
    }

    /// <summary>
    /// Gets the expression a declaration gives a value from.
    /// </summary>
    /// <param name="declaration">The declaration to read.</param>
    /// <returns>The expression, or <see langword="null"/> when the declaration gives none.</returns>
    static ExpressionSyntax? DeclaredValueOf(SyntaxNode declaration) => declaration switch
    {
        VariableDeclaratorSyntax variable => variable.Initializer?.Value,
        PropertyDeclarationSyntax property => property.Initializer?.Value ?? property.ExpressionBody?.Expression,
        _ => null
    };

    /// <summary>
    /// Determines whether an expression states no value at all.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True when it is a placeholder rather than a value.</returns>
    /// <remarks>
    /// A member a specification fills in later is declared <c>= null!</c> or <c>= default</c>, because the compiler
    /// insists on a value and the specification has none to give yet. Counting that as one of the places the value
    /// is given would make every such member look like it was given two values and leave the scenario out - which is
    /// the shape nearly every specification holding a value is written in.
    /// </remarks>
    static bool StatesNothing(ExpressionSyntax expression) =>
        MappingSourceReader.Unwrap(expression) is
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression or (int)SyntaxKind.DefaultLiteralExpression } or
            DefaultExpressionSyntax;

    /// <summary>
    /// Determines whether a value is given where it is given every run, exactly once.
    /// </summary>
    /// <param name="given">The expression the value is given from.</param>
    /// <returns>True when nothing between it and the member it is written in makes it conditional or repeated.</returns>
    static bool Unconditional(ExpressionSyntax given) =>
        given.FirstAncestorOrSelf<MemberDeclarationSyntax>() is not { } member || StepsTaken.Always(given, member);

    static bool Names(ExpressionSyntax expression, ISymbol symbol) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText == symbol.Name,
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText == symbol.Name,
        _ => false
    };

    bool ReadStability(ISymbol symbol, Compilation compilation)
    {
        if (symbol is IFieldSymbol { IsReadOnly: true } or IFieldSymbol { IsConst: true })
        {
            return true;
        }

        if (symbol is IPropertySymbol { ContainingType.IsRecord: true, DeclaringSyntaxReferences.Length: > 0 } &&
            symbol.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is ParameterSyntax { Parent.Parent: RecordDeclarationSyntax }))
        {
            var recordWrites = WritesIn(compilation);

            return !recordWrites.Assignments.ContainsKey(symbol) && !recordWrites.Mutations.Contains(symbol);
        }

        if (symbol is IPropertySymbol && (symbol.DeclaringSyntaxReferences.Length == 0 ||
            symbol.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is not PropertyDeclarationSyntax
            { ExpressionBody: null, AccessorList: { } accessors } || accessors.Accessors.Any(accessor => accessor.Body is not null || accessor.ExpressionBody is not null))))
        {
            return false;
        }

        var writes = WritesIn(compilation);
        var values = symbol.DeclaringSyntaxReferences.Select(reference => DeclaredValueOf(reference.GetSyntax()))
            .OfType<ExpressionSyntax>().Where(value => !StatesNothing(value))
            .Concat(writes.Assignments.GetValueOrDefault(symbol, []).Select(assignment => assignment.Right));
        if (symbol is not (IFieldSymbol or ILocalSymbol or IPropertySymbol) || values.Take(2).ToList() is not [var given] || !Unconditional(given))
        {
            return false;
        }

        return !writes.Mutations.Contains(symbol);
    }

    /// <summary>
    /// Gets the construction a value a specification holds was put together by.
    /// </summary>
    /// <param name="symbol">The member or local the step names.</param>
    /// <returns>The <see cref="HeldConstruction"/>, or <see langword="null"/> when it cannot be read.</returns>
    HeldConstruction? Held(ISymbol? symbol)
    {
        if (symbol is not (IFieldSymbol or IPropertySymbol or ILocalSymbol))
        {
            return null;
        }

        // Two are taken rather than one so that a second place the value is given makes the first unreadable. A
        // value given in two places held different values in different runs, and the source text does not say which
        // one the step saw.
        if (GivenTo(symbol).Take(2).ToList() is not [var given] ||
            MappingSourceReader.Unwrap(given) is not BaseObjectCreationExpressionSyntax creation ||
            !Unconditional(given))
        {
            return null;
        }

        // No model for the tree means the construction lives somewhere this analysis cannot read, so the step is
        // left unread rather than read through the wrong model.
        return models.For(creation.SyntaxTree) is { } model ? new(creation, model) : null;
    }

    /// <summary>
    /// Gets every expression a value is given, in declaration order.
    /// </summary>
    /// <param name="symbol">The member or local to follow.</param>
    /// <returns>The expressions.</returns>
    IEnumerable<ExpressionSyntax> GivenTo(ISymbol symbol) =>
        symbol.DeclaringSyntaxReferences
            .Select(_ => DeclaredValueOf(_.GetSyntax()))
            .OfType<ExpressionSyntax>()
            .Concat(AssignedValuesTo(symbol))
            .Where(given => !StatesNothing(given));

    /// <summary>
    /// Gets the expression of every assignment to a value, wherever the type declaring it writes one.
    /// </summary>
    /// <param name="symbol">The member or local to follow.</param>
    /// <returns>The expressions assigned.</returns>
    IEnumerable<ExpressionSyntax> AssignedValuesTo(ISymbol symbol)
    {
        foreach (var reference in symbol.ContainingType?.DeclaringSyntaxReferences ?? [])
        {
            if (models.For(reference.SyntaxTree) is not { } model)
            {
                continue;
            }

            var declaration = reference.GetSyntax();
            foreach (var assignment in WritesIn(model.Compilation).Assignments.GetValueOrDefault(symbol, [])
                .Where(assignment => assignment.SyntaxTree == reference.SyntaxTree && declaration.Span.Contains(assignment.Span)))
            {
                yield return assignment.Right;
            }
        }
    }

    Writes WritesIn(Compilation compilation)
    {
        if (_writes.TryGetValue(compilation, out var cached))
        {
            return cached;
        }

        var writes = new Writes();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var candidates = tree.GetRoot().DescendantNodes().Where(node => node is AssignmentExpressionSyntax ||
                node.RawKind is (int)SyntaxKind.PreIncrementExpression or (int)SyntaxKind.PostIncrementExpression or
                    (int)SyntaxKind.PreDecrementExpression or (int)SyntaxKind.PostDecrementExpression ||
                node is ArgumentSyntax { RefKindKeyword.RawKind: not 0 }).ToList();
            if (candidates.Count == 0 || models.For(tree) is not { } model)
            {
                continue;
            }

            foreach (var candidate in candidates)
            {
                var target = candidate switch
                {
                    AssignmentExpressionSyntax assignment => assignment.Left,
                    ArgumentSyntax argument => argument.Expression,
                    _ => candidate.ChildNodes().OfType<ExpressionSyntax>().FirstOrDefault()
                };
                if (target is not (IdentifierNameSyntax or MemberAccessExpressionSyntax) ||
                    model.GetSymbolInfo(target).Symbol is not { } symbol || !Names(target, symbol))
                {
                    continue;
                }

                if (candidate is AssignmentExpressionSyntax assigned)
                {
                    if (!writes.Assignments.TryGetValue(symbol, out var assignments))
                    {
                        writes.Assignments[symbol] = assignments = [];
                    }

                    assignments.Add(assigned);
                }
                else
                {
                    writes.Mutations.Add(symbol);
                }
            }
        }

        return _writes[compilation] = writes;
    }

    sealed class Writes
    {
        public Dictionary<ISymbol, List<AssignmentExpressionSyntax>> Assignments { get; } = new(SymbolEqualityComparer.Default);
        public HashSet<ISymbol> Mutations { get; } = new(SymbolEqualityComparer.Default);
    }
}
