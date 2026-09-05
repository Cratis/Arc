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
    IEnumerable<ExpressionSyntax> AssignedValuesTo(ISymbol symbol) =>
        (symbol.ContainingType?.DeclaringSyntaxReferences ?? [])
            .Select(_ => _.GetSyntax())
            .SelectMany(declaration => declaration.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            .Where(assignment => Assigns(assignment, symbol))
            .Select(assignment => assignment.Right);

    /// <summary>
    /// Determines whether an assignment writes to a value.
    /// </summary>
    /// <param name="assignment">The assignment to check.</param>
    /// <param name="symbol">The member or local it would write to.</param>
    /// <returns>True when it writes to it.</returns>
    bool Assigns(AssignmentExpressionSyntax assignment, ISymbol symbol) =>
        models.For(assignment.SyntaxTree) is { } model &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, symbol);
}
