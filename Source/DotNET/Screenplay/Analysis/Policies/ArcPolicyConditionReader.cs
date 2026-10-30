// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Recovers a native Arc policy only when its whole synchronous condition is stated over the caller's principal.
/// </summary>
/// <param name="compilations">The analyzed application projects.</param>
internal sealed class ArcPolicyConditionReader(IReadOnlyList<Compilation> compilations)
{
    const string PolicyInterface = "Cratis.Arc.Authorization.IAuthorizationPolicy";
    const string PolicyContext = "Cratis.Arc.Authorization.AuthorizationPolicyContext";
    const string Principal = "System.Security.Claims.ClaimsPrincipal";
    const string SynchronousExpression = "IsAuthorized is not a single synchronous boolean expression";
    readonly SemanticModels _models = new(compilations);

    /// <summary>
    /// Reads the complete principal condition, or explains why the policy cannot be recovered.
    /// </summary>
    /// <param name="registration">The native policy registration.</param>
    /// <returns>The recovered requirement or a refusal reason.</returns>
    internal (PolicyRequirementModel? Requirement, string? Reason) Read(ArcPolicyRegistration registration)
    {
        if (registration.PolicyType is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } type)
        {
            return (null, "its registration does not name a concrete policy class");
        }

        var member = type.AllInterfaces.FirstOrDefault(item => item.Is(PolicyInterface))?.GetMembers("IsAuthorized").OfType<IMethodSymbol>().FirstOrDefault();
        var implementation = member is null ? null : type.FindImplementationForInterfaceMember(member) as IMethodSymbol;
        var syntax = DeclarationOf(implementation);
        if (syntax is null || _models.For(syntax.SyntaxTree) is not { } model)
        {
            return (null, "its implementation is not in the analyzed source");
        }

        return ReadMethod(syntax, model);
    }

    static (PolicyRequirementModel? Requirement, string? Reason) ReadMethod(MethodDeclarationSyntax syntax, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(syntax) is not { IsAsync: false, Parameters.Length: 2 } method)
        {
            return (null, SynchronousExpression);
        }

        var returned = ReturnedValue(syntax, method.Parameters[1], model);
        var expression = BooleanResult(returned, model);

        return expression is null ? (null, SynchronousExpression) : Condition(expression, method.Parameters[0], model);
    }

    static ExpressionSyntax? ReturnedValue(MethodDeclarationSyntax syntax, IParameterSymbol cancellation, SemanticModel model)
    {
        if (syntax.ExpressionBody is { } arrow)
        {
            return arrow.Expression;
        }

        return syntax.Body?.Statements.ToArray() switch
        {
            [ReturnStatementSyntax returned] => returned.Expression,
            [ExpressionStatementSyntax check, ReturnStatementSyntax returned] when IsCancellationCheck(check, cancellation, model) => returned.Expression,
            _ => null
        };
    }

    static bool IsCancellationCheck(ExpressionStatementSyntax check, IParameterSymbol cancellation, SemanticModel model) =>
        model.GetOperation(check.Expression) is IInvocationOperation { Arguments.Length: 0 } call &&
        call.TargetMethod.Name == "ThrowIfCancellationRequested" && call.TargetMethod.ContainingType.Is("System.Threading.CancellationToken") &&
        IsParameter(call.Instance, cancellation);

    static ExpressionSyntax? BooleanResult(ExpressionSyntax? returned, SemanticModel model)
    {
        if (returned is null)
        {
            return null;
        }

        return model.GetOperation(Unwrap(returned)) switch
        {
            IInvocationOperation call when IsFromResult(call.TargetMethod) => call.Arguments.SingleOrDefault()?.Value.Syntax as ExpressionSyntax,
            IObjectCreationOperation creation when IsBooleanConstructor(creation.Constructor) => creation.Arguments.SingleOrDefault()?.Value.Syntax as ExpressionSyntax,
            _ => null
        };
    }

    static bool IsFromResult(IMethodSymbol method) =>
        method.Name == "FromResult" && method.ContainingType.Is("System.Threading.Tasks.ValueTask") &&
        method.TypeArguments is [{ SpecialType: SpecialType.System_Boolean }];

    static bool IsBooleanConstructor(IMethodSymbol? constructor) =>
        constructor?.ContainingType.Is("System.Threading.Tasks.ValueTask`1") == true &&
        constructor.Parameters is [{ Type.SpecialType: SpecialType.System_Boolean }];

    static (PolicyRequirementModel? Requirement, string? Reason) Condition(ExpressionSyntax expression, IParameterSymbol context, SemanticModel model)
    {
        expression = Unwrap(expression);
        if (expression is BinaryExpressionSyntax binary && IsCombination(binary))
        {
            return Combine(binary, context, model);
        }

        if (expression is InvocationExpressionSyntax invocation && PrincipalCall(invocation, context, model) is { } requirement)
        {
            return (requirement, null);
        }

        return IsAuthenticated(expression, context, model)
            ? (AuthenticatedRequirement.Instance, null)
            : (null, $"'{expression.Kind()}' has no counterpart in a policy condition");
    }

    static bool IsCombination(BinaryExpressionSyntax expression) =>
        expression.IsKind(SyntaxKind.LogicalAndExpression) || expression.IsKind(SyntaxKind.LogicalOrExpression);

    static (PolicyRequirementModel? Requirement, string? Reason) Combine(BinaryExpressionSyntax binary, IParameterSymbol context, SemanticModel model)
    {
        var left = Condition(binary.Left, context, model);
        var right = Condition(binary.Right, context, model);

        return left.Requirement is null ? left : right.Requirement is null ? right :
            (new CombinedRequirement(left.Requirement, binary.IsKind(SyntaxKind.LogicalOrExpression), right.Requirement), null);
    }

    static PolicyRequirementModel? PrincipalCall(InvocationExpressionSyntax syntax, IParameterSymbol context, SemanticModel model)
    {
        if (model.GetOperation(syntax) is not IInvocationOperation call || !IsPrincipalCall(call, context))
        {
            return null;
        }

        var values = ConstantStrings(call);

        return (call.TargetMethod.Name, values) switch
        {
            ("IsInRole", [var role]) => new RoleRequirement(role),
            ("HasClaim", [var claim, var value]) => new ClaimRequirement(claim, value),
            _ => null
        };
    }

    static bool IsPrincipalCall(IInvocationOperation call, IParameterSymbol context) =>
        call.TargetMethod.ContainingType.Is(Principal) && IsPrincipal(call.Instance, context);

    static string[] ConstantStrings(IInvocationOperation call)
    {
        if (call.TargetMethod.Parameters.Any(parameter => parameter.Type.SpecialType != SpecialType.System_String))
        {
            return [];
        }

        var values = call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal)
            .Select(argument => argument.Value.ConstantValue is { HasValue: true, Value: string value } ? value : null).ToArray();

        return values.All(value => !string.IsNullOrWhiteSpace(value)) ? values.Select(value => value!).ToArray() : [];
    }

    static bool IsPrincipal(IOperation? operation, IParameterSymbol context) =>
        operation is IPropertyReferenceOperation property && property.Property.Name == "Principal" &&
        property.Property.ContainingType.Is(PolicyContext) && IsParameter(property.Instance, context);

    static bool IsParameter(IOperation? operation, IParameterSymbol parameter) =>
        operation is IParameterReferenceOperation receiver && SymbolEqualityComparer.Default.Equals(receiver.Parameter, parameter);

    static bool IsAuthenticated(ExpressionSyntax expression, IParameterSymbol context, SemanticModel model)
    {
        var tested = expression switch
        {
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression) && Unwrap(binary.Right).IsKind(SyntaxKind.TrueLiteralExpression) => binary.Left,
            IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax pattern } test when pattern.Expression.IsKind(SyntaxKind.TrueLiteralExpression) => test.Expression,
            _ => null
        };

        return tested is not null && IsIdentityAuthentication(Unwrap(tested), context, model);
    }

    static bool IsIdentityAuthentication(ExpressionSyntax expression, IParameterSymbol context, SemanticModel model) =>
        expression is ConditionalAccessExpressionSyntax access &&
        IsProperty(access.WhenNotNull, "IsAuthenticated", "System.Security.Principal.IIdentity", model) &&
        model.GetOperation(access.Expression) is IPropertyReferenceOperation identity && identity.Property.Name == "Identity" &&
        identity.Property.ContainingType.Is(Principal) && IsPrincipal(identity.Instance, context);

    static bool IsProperty(SyntaxNode syntax, string name, string type, SemanticModel model) =>
        model.GetSymbolInfo(syntax).Symbol is IPropertySymbol property && property.Name == name && property.ContainingType.Is(type);

    static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    MethodDeclarationSyntax? DeclarationOf(IMethodSymbol? method)
    {
        if (method is null)
        {
            return null;
        }

        var source = method.DeclaringSyntaxReferences.IsEmpty ? _models.SourceOf(method) : method;

        return source?.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<MethodDeclarationSyntax>().FirstOrDefault();
    }
}
