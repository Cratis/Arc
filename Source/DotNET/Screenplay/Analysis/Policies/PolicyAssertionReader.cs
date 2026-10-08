// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Reads a guarded command-resource assertion without interpreting arbitrary C#.
/// </summary>
/// <param name="compilations">All application compilations, used to check every policy use.</param>
public class PolicyAssertionReader(IReadOnlyList<Compilation> compilations)
{
    readonly ScreenplayNaming _naming = new();

    /// <summary>
    /// Reads a single assertion, failing atomically when any operand is unreadable.
    /// </summary>
    /// <param name="expression">The assertion lambda.</param>
    /// <param name="registration">The registered policy.</param>
    /// <returns>The faithful condition, or null.</returns>
    public PolicyRequirementModel? Read(ExpressionSyntax expression, PolicyRegistration registration)
    {
        if (expression is not LambdaExpressionSyntax lambda || lambda.AsyncKeyword != default)
        {
            return null;
        }

        var context = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [var parameter] } => parameter,
            _ => null
        };
        if (context is null)
        {
            return null;
        }

        var body = lambda.ExpressionBody ?? (lambda.Block?.Statements is [ReturnStatementSyntax returned] ? returned.Expression : null);
        var model = registration.SemanticModel;
        var clauses = Conjuncts(Unwrap(body)).ToArray();
        if (clauses.Length < 2 ||
            clauses[0] is not IsPatternExpressionSyntax { Expression: MemberAccessExpressionSyntax resource, Pattern: RecursivePatternSyntax pattern } ||
            !IsContextMember(resource, context, "Resource", model) ||
            pattern.Type is null || model.GetTypeInfo(pattern.Type).Type?.Is("Cratis.Arc.Commands.CommandContext") != true ||
            pattern.PropertyPatternClause?.Subpatterns is not [var subpattern] || subpattern.NameColon?.Name.Identifier.ValueText != "Command" ||
            subpattern.Pattern is not DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax variable } commandPattern ||
            model.GetTypeInfo(commandPattern.Type).Type is not INamedTypeSymbol command || !CommandReader.IsCommand(command) ||
            !OnlyUsedBy(registration.Name, command))
        {
            return new QueryPolicyAssertionReader(compilations).Read(clauses, context, registration);
        }

        var conditions = clauses.Skip(1).Select(clause => Condition(clause, context, variable, command, registration)).ToArray();

        return conditions.Any(condition => condition is null) ? null : conditions.Cast<PolicyRequirementModel>()
            .Aggregate((left, right) => new CombinedRequirement(left, false, right));
    }

    /// <summary>
    /// Checks a member against the actual authorization context contract.
    /// </summary>
    /// <param name="access">The member access.</param>
    /// <param name="context">The lambda parameter.</param>
    /// <param name="name">The expected contract member.</param>
    /// <param name="model">The source semantic model.</param>
    /// <returns>Whether the access reads that contract member on the lambda parameter.</returns>
    internal static bool IsContextMember(MemberAccessExpressionSyntax access, ParameterSyntax context, string name, SemanticModel model) =>
        model.GetSymbolInfo(access).Symbol is IPropertySymbol property && property.Name == name &&
        property.ContainingType.Is("Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext") &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(access.Expression).Symbol, model.GetDeclaredSymbol(context));

    /// <summary>
    /// Gets an explicitly supplied argument by its bound parameter, regardless of source ordering.
    /// </summary>
    /// <param name="invocation">The invocation to read.</param>
    /// <param name="ordinal">The parameter's position in the method signature.</param>
    /// <param name="model">The source semantic model.</param>
    /// <returns>The argument expression, or null when it is not explicitly bound.</returns>
    internal static ExpressionSyntax? ArgumentOf(InvocationExpressionSyntax invocation, int ordinal, SemanticModel model) =>
        model.GetOperation(invocation) is IInvocationOperation operation &&
        operation.Arguments.SingleOrDefault(argument => argument.Parameter?.Ordinal == ordinal) is { IsImplicit: false, Syntax: ArgumentSyntax syntax }
            ? syntax.Expression
            : null;

    static IEnumerable<ExpressionSyntax> Conjuncts(ExpressionSyntax? expression) => expression switch
    {
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) => Conjuncts(Unwrap(binary.Left)).Concat(Conjuncts(Unwrap(binary.Right))),
        null => [],
        _ => [expression]
    };

    static ExpressionSyntax? Unwrap(ExpressionSyntax? expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    PolicyRequirementModel? Condition(ExpressionSyntax expression, ParameterSyntax context, SingleVariableDesignationSyntax variable, INamedTypeSymbol command, PolicyRegistration registration)
    {
        var model = registration.SemanticModel;
        expression = Unwrap(expression)!;
        if (expression is BinaryExpressionSyntax binary && (binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression)))
        {
            var left = Condition(binary.Left, context, variable, command, registration);
            var right = Condition(binary.Right, context, variable, command, registration);

            return left is null || right is null ? null : new CombinedRequirement(left, binary.IsKind(SyntaxKind.LogicalOrExpression), right);
        }

        if (expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax call } invocation ||
            call.Expression is not MemberAccessExpressionSyntax user || !IsContextMember(user, context, "User", model) ||
            model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { Name: "HasClaim", Parameters.Length: 2 } method ||
            !method.ContainingType.Is("System.Security.Claims.ClaimsPrincipal") || method.Parameters.Any(parameter => parameter.Type.SpecialType != SpecialType.System_String) ||
            ArgumentOf(invocation, 0, model) is not { } claimArgument || ArgumentOf(invocation, 1, model) is not { } targetArgument ||
            model.GetConstantValue(claimArgument).Value is not string claim || string.IsNullOrWhiteSpace(claim))
        {
            return null;
        }

        var path = PropertyPath(targetArgument, variable, model);
        if (path is null)
        {
            return null;
        }

        var identifier = new CommandIdentifierReader(new SemanticModels([model.Compilation]), new ScreenplayDiagnostics()).Read(command, registration.Location);
        var emptyHandler = command.GetMembers("Handle").OfType<IMethodSymbol>().All(handler => handler.ReturnsVoid &&
            handler.DeclaringSyntaxReferences.All(reference => reference.GetSyntax() is MethodDeclarationSyntax { Body.Statements.Count: 0 }));

        return new ClaimTargetRequirement(claim, path, emptyHandler && identifier is not null && path == _naming.ToPropertyName(identifier));
    }

    string? PropertyPath(ExpressionSyntax expression, SingleVariableDesignationSyntax variable, SemanticModel model)
    {
        var target = expression;
        var parts = new List<string>();
        while (expression is MemberAccessExpressionSyntax access)
        {
            if (model.GetSymbolInfo(access).Symbol is not IPropertySymbol property || property.NullableAnnotation == NullableAnnotation.Annotated ||
                property.ContainingType is not { IsRecord: true } || property.DeclaringSyntaxReferences.IsEmpty ||
                property.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is not ParameterSyntax))
            {
                return null;
            }

            parts.Insert(0, _naming.ToPropertyName(property.Name));
            expression = access.Expression;
        }

        return parts.Count > 0 && model.GetTypeInfo(target).Type?.SpecialType == SpecialType.System_String &&
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(expression).Symbol, model.GetDeclaredSymbol(variable))
            ? string.Join('.', parts)
            : null;
    }

    bool OnlyUsedBy(string policy, INamedTypeSymbol command)
    {
        var uses = compilations.SelectMany(compilation => ArtifactCatalog.From(compilation).Types)
            .SelectMany(type => new ISymbol[] { type }.Concat(type.GetMembers().OfType<IMethodSymbol>()))
            .Where(symbol => AuthorizationReader.Read(symbol)?.Policies.Contains(policy, StringComparer.Ordinal) == true).ToArray();

        return uses.Length > 0 && uses.All(symbol => symbol is INamedTypeSymbol type && type.ToDisplayString() == command.ToDisplayString() &&
            type.ContainingAssembly.Identity.Name == command.ContainingAssembly.Identity.Name);
    }
}
