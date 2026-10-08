// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Queries;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Reads a query-key claim comparison guarded by argument presence and its string type.
/// </summary>
/// <param name="compilations">All application compilations, for checking every use of the policy.</param>
public class QueryPolicyAssertionReader(IReadOnlyList<Compilation> compilations)
{
    /// <summary>
    /// Reads the safe query-context dictionary lookup shape without inferring a key from a variable's name.
    /// </summary>
    /// <param name="clauses">The assertion's top-level conjunctions.</param>
    /// <param name="context">The authorization-context parameter.</param>
    /// <param name="registration">The policy being read.</param>
    /// <returns>The requirement, or null when its argument or use sites are not proven.</returns>
    public PolicyRequirementModel? Read(ExpressionSyntax[] clauses, ParameterSyntax context, PolicyRegistration registration)
    {
        var model = registration.SemanticModel;
        if (clauses is not [IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax resource, Pattern: RecursivePatternSyntax pattern },
            InvocationExpressionSyntax lookup, IsPatternExpressionSyntax valueCheck, InvocationExpressionSyntax claimCheck] ||
            !PolicyAssertionReader.IsContextMember(resource, context, "Resource", model) ||
            pattern.Type is null || model.GetTypeInfo(pattern.Type).Type?.Is(WellKnownTypeNames.QueryContext) != true ||
            pattern.PropertyPatternClause?.Subpatterns is not [var subpattern] || subpattern.NameColon?.Name.Identifier.ValueText != "Arguments" ||
            subpattern.Pattern is not RecursivePatternSyntax { Type: null, PropertyPatternClause.Subpatterns.Count: 0, Designation: SingleVariableDesignationSyntax arguments } ||
            lookup.Expression is not MemberAccessExpressionSyntax lookupMember || !SameVariable(lookupMember.Expression, arguments, model) ||
            model.GetSymbolInfo(lookup).Symbol is not IMethodSymbol { Name: "TryGetValue" } lookupMethod ||
            !lookupMethod.ContainingType.Is("System.Collections.Generic.Dictionary`2") ||
            lookup.ArgumentList.Arguments is not [var keyArgument, var valueArgument] ||
            model.GetConstantValue(keyArgument.Expression).Value is not string key ||
            valueArgument.Expression is not DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax value } ||
            !SameVariable(valueCheck.Expression, value, model) ||
            valueCheck.Pattern is not DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax identifier } valuePattern ||
            model.GetTypeInfo(valuePattern.Type).Type?.SpecialType != SpecialType.System_String ||
            claimCheck.Expression is not MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax user } ||
            !PolicyAssertionReader.IsContextMember(user, context, "User", model) ||
            model.GetSymbolInfo(claimCheck).Symbol is not IMethodSymbol { Name: "HasClaim", Parameters.Length: 2 } claimMethod ||
            !claimMethod.ContainingType.Is("System.Security.Claims.ClaimsPrincipal") ||
            claimCheck.ArgumentList.Arguments is not [var claimArgument, var targetArgument] ||
            model.GetConstantValue(claimArgument.Expression).Value is not string claim || string.IsNullOrWhiteSpace(claim) ||
            !SameVariable(targetArgument.Expression, identifier, model) || !OnlyUsedWithKey(registration.Name, key))
        {
            return null;
        }

        return new ClaimTargetRequirement(claim, new ScreenplayNaming().ToPropertyName(key), true);
    }

    static bool SameVariable(ExpressionSyntax expression, SingleVariableDesignationSyntax variable, SemanticModel model) =>
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(expression).Symbol, model.GetDeclaredSymbol(variable));

    static bool HasKey(IMethodSymbol method, string key)
    {
        var parameters = method.Parameters.Where(QueryReader.IsInput).ToArray();
        if (parameters is not [var parameter] || parameter.HasExplicitDefaultValue ||
            parameter.Type.SpecialType != SpecialType.System_String || parameter.NullableAnnotation == NullableAnnotation.Annotated ||
            !string.Equals(parameter.Name, key, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var collection = false;
        var returned = QueryReturnTypes.Unwrap(method.ReturnType, ref collection);
        var naming = new ScreenplayNaming();

        return !collection && !QueryReturnTypes.IsObservable(method.ReturnType) && returned.DeclaredProperties().Any(property =>
            naming.ToPropertyName(property.Name) == naming.ToPropertyName(parameter.Name) && property.Type.SpecialType == SpecialType.System_String);
    }

    bool OnlyUsedWithKey(string policy, string key)
    {
        var uses = new List<IMethodSymbol>();
        foreach (var type in compilations.SelectMany(compilation => ArtifactCatalog.From(compilation).Types))
        {
            if (!QueryReader.IsReadModel(type))
            {
                if (AuthorizationReader.Read(type)?.Policies.Contains(policy, StringComparer.Ordinal) == true ||
                    type.GetMembers().OfType<IMethodSymbol>().Any(method => AuthorizationReader.Read(method)?.Policies.Contains(policy, StringComparer.Ordinal) == true))
                {
                    return false;
                }

                continue;
            }

            uses.AddRange(QueryReader.MethodsOf(type).Where(method => AuthorizationReader.Read(method, type)?.Policies.Contains(policy, StringComparer.Ordinal) == true));
        }

        return uses.Count > 0 && uses.TrueForAll(method => HasKey(method, key));
    }
}
