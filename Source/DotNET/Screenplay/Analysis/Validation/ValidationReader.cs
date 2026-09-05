// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Finds the rule chains a validator's constructor declares.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every constructor is read through.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Reading the constructor recovers what the runtime rule descriptor loses - which rules were declared for each
/// element of a collection, and which comparisons were made against something other than a number.
/// <para>
/// A validator declaring rules for a concept the project below it holds is written wherever it is, so which model
/// reads its constructor is asked rather than assumed.
/// </para>
/// </remarks>
public class ValidationReader(SemanticModels models, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The call declaring a rule for a property.
    /// </summary>
    public const string RuleFor = "RuleFor";

    /// <summary>
    /// The call declaring a rule for each element of a collection property.
    /// </summary>
    public const string RuleForEach = "RuleForEach";

    readonly ValidationChainReader _chains = new(diagnostics);

    /// <summary>
    /// Determines whether a type is a validator, and of what.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The validated type, or <see langword="null"/> when the type is not a validator.</returns>
    public static ITypeSymbol? ValidatedTypeOf(ITypeSymbol type) =>
        type.FindBase(WellKnownTypeNames.AbstractValidator)?.TypeArguments[0];

    /// <summary>
    /// Reads every rule a validator declares.
    /// </summary>
    /// <param name="validator">The type declaring the validator.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <returns>The rules, in the order the source declares them.</returns>
    public IEnumerable<ValidationRuleModel> Read(INamedTypeSymbol validator, string location)
    {
        var rules = new List<ValidationRuleModel>();

        foreach (var constructor in validator.InstanceConstructors.Where(_ => _.DeclaringSyntaxReferences.Length > 0))
        {
            foreach (var reference in constructor.DeclaringSyntaxReferences)
            {
                ReadDeclaration(reference.GetSyntax(), location, rules);
            }
        }

        return rules;
    }

    /// <summary>
    /// Reads every rule chain within one constructor declaration.
    /// </summary>
    /// <param name="declaration">The declaration to read.</param>
    /// <param name="location">Where the validator lives, for use in diagnostics.</param>
    /// <param name="rules">The rules collected so far.</param>
    void ReadDeclaration(SyntaxNode declaration, string location, List<ValidationRuleModel> rules)
    {
        if (models.For(declaration.SyntaxTree) is not { } semanticModel)
        {
            return;
        }

        foreach (var statement in declaration.DescendantNodes().OfType<ExpressionStatementSyntax>())
        {
            var chain = InvocationChain.Unwind(statement.Expression);
            var forEach = chain is not null && string.Equals(chain.RootName, RuleForEach, StringComparison.Ordinal);

            if (chain is null || (!forEach && !string.Equals(chain.RootName, RuleFor, StringComparison.Ordinal)))
            {
                continue;
            }

            _chains.Read(chain, forEach, semanticModel, location, rules);
        }
    }
}
