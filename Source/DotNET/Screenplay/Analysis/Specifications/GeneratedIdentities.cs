// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Recognizes an expression yielding an identity made on the spot.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every declaration is read through.</param>
/// <remarks>
/// Every other value a scenario states that cannot be read is a gap: the source says something concrete and the
/// document says less than the source. A fresh identity is not that. It has no value to state - the whole point of
/// making one is that nobody wrote it down - so a document leaving it out says exactly as much as the source does,
/// and a report saying it was left out describes a difference that is not there.
/// <para>
/// What counts as one is deliberately narrow. A static method taking nothing and handing back the type it is declared
/// on has nothing to derive a value from, so making one up is all it can be doing; a method that computes takes what
/// it computes from. Narrowing further to a <c>Guid</c> and to the concepts wrapping one value keeps it to the types
/// an identity is really written as, so a value that is computed and is not an identity is still reported.
/// </para>
/// <para>
/// The identity is followed back to where it was made, because a scenario rests two steps on the same one and
/// therefore holds it in a field rather than writing it twice. Following the initializer of that field is what makes
/// the recognition about the value rather than about how close to the step it happens to be written.
/// </para>
/// <para>
/// Where that member is declared is not where the scenario is. A sentinel identity or an enumeration member the
/// scenario names is routinely declared in the contracts project below it, so the declaration followed belongs to
/// another project's compilation - which is why it is read through the models of the whole application rather than
/// through any one of them.
/// </para>
/// </remarks>
public class GeneratedIdentities(SemanticModels models)
{
    /// <summary>
    /// The fully qualified metadata name of the type every generated identity is a value of, or wraps.
    /// </summary>
    public const string Uuid = "System.Guid";

    /// <summary>
    /// The names a method making an identity is declared under.
    /// </summary>
    public static readonly string[] Factories = ["New", "NewGuid"];

    /// <summary>
    /// Determines whether an expression yields an identity made on the spot.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>True when the expression yields one.</returns>
    public bool Yields(ExpressionSyntax expression, SemanticModel semanticModel) =>
        Yields(expression, semanticModel, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>
    /// Determines whether a method makes an identity.
    /// </summary>
    /// <param name="method">The method being called.</param>
    /// <returns>True when calling it makes one.</returns>
    static bool Makes(IMethodSymbol? method) =>
        method is { IsStatic: true, Parameters.Length: 0, ContainingType: { } declaring } &&
        Array.Exists(Factories, _ => string.Equals(method.Name, _, StringComparison.Ordinal)) &&
        SymbolEqualityComparer.Default.Equals(method.ReturnType, declaring) &&
        IsIdentity(declaring);

    /// <summary>
    /// Determines whether a type is one an identity is written as.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is one.</returns>
    static bool IsIdentity(ITypeSymbol type) => type.Is(Uuid) || type.FindBase(WellKnownTypeNames.ConceptAs) is not null;

    /// <summary>
    /// Gets the expression a declaration gives a member its value from.
    /// </summary>
    /// <param name="declaration">The declaration to read.</param>
    /// <returns>The expression, or <see langword="null"/> when the declaration gives none.</returns>
    static ExpressionSyntax? ValueOf(SyntaxNode declaration) => declaration switch
    {
        VariableDeclaratorSyntax variable => variable.Initializer?.Value,
        PropertyDeclarationSyntax property => property.Initializer?.Value ?? property.ExpressionBody?.Expression,
        _ => null
    };

    /// <summary>
    /// Determines whether an expression yields an identity made on the spot.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="followed">The members already followed, so that nothing is followed twice.</param>
    /// <returns>True when the expression yields one.</returns>
    bool Yields(ExpressionSyntax expression, SemanticModel semanticModel, ISet<string> followed)
    {
        var unwrapped = MappingSourceReader.Unwrap(expression);
        if (unwrapped is InvocationExpressionSyntax invocation)
        {
            return Makes(semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol);
        }

        return unwrapped is IdentifierNameSyntax or MemberAccessExpressionSyntax &&
            Holds(semanticModel.GetSymbolInfo(unwrapped).Symbol, followed);
    }

    /// <summary>
    /// Determines whether a member holds an identity made on the spot.
    /// </summary>
    /// <param name="symbol">The member the expression names.</param>
    /// <param name="followed">The members already followed.</param>
    /// <returns>True when the member holds one.</returns>
    /// <remarks>
    /// A member declared in a project that was not handed over holds nothing this can read, so it is not one of these.
    /// The value naming it is then stated as one nothing was recovered from, which says less than the source does but
    /// says it out loud - and is what the reader is owed when a project is missing.
    /// </remarks>
    bool Holds(ISymbol? symbol, ISet<string> followed)
    {
        if (symbol is not (IFieldSymbol or IPropertySymbol) || !followed.Add(symbol.ToDisplayString()))
        {
            return false;
        }

        return symbol.DeclaringSyntaxReferences
            .Select(_ => ValueOf(_.GetSyntax()))
            .OfType<ExpressionSyntax>()
            .Any(value => models.For(value.SyntaxTree) is { } semanticModel && Yields(value, semanticModel, followed));
    }
}
