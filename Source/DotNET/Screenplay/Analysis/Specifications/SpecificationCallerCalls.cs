// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Recognizes caller setup and competing authorization service calls by resolved metadata identity.
/// </summary>
internal static class SpecificationCallerCalls
{
    const string Accessor = "Cratis.Arc.Authorization.ICurrentPrincipalAccessor";
    static readonly string[] _authorizationTypes =
    [
        Accessor,
        "Cratis.Arc.Authorization.ICurrentPrincipalOverride",
        "Cratis.Arc.Authorization.IAuthorizationPolicyRuntime",
        "Cratis.Arc.Authorization.ISystemExecution"
    ];

    /// <summary>
    /// Recognizes the documented substitute factory.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>Whether this constructs the accessor substitute.</returns>
    internal static bool IsFactory(SpecificationCallerCall call) => call.Method?.Name == "For" && IsAccessorFactory(call.Method);

    /// <summary>
    /// Recognizes a configuration of the accessor's Current property.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>Whether this configures Current.</returns>
    internal static bool IsConfiguration(SpecificationCallerCall call) =>
        call.Method?.ContainingType.Is("NSubstitute.SubstituteExtensions") == true && HasCurrentReceiver(call);

    /// <summary>
    /// Recognizes calls touching other authorization services too.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>Whether the call affects authorization.</returns>
    internal static bool IsAuthorizationUse(SpecificationCallerCall call) => IsAuthorizationMethod(call.Method) || HasAuthorizationArgument(call);

    /// <summary>
    /// Gets the Current property configured by a Returns call.
    /// </summary>
    /// <param name="call">The source call.</param>
    /// <returns>The Current property access, or null.</returns>
    internal static MemberAccessExpressionSyntax? CurrentReceiver(SpecificationCallerCall call) =>
        call.Syntax.Expression is MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax current } && current.Name.Identifier.ValueText == "Current"
            ? current : null;

    static bool IsAuthorizationMethod(IMethodSymbol? method) =>
        IsAuthorizationType(method?.ContainingType) || HasAuthorizationSignature(method);

    static bool HasAuthorizationSignature(IMethodSymbol? method) =>
        IsAuthorizationType(method?.ReturnType) || method?.TypeArguments.Any(IsAuthorizationType) == true;

    static bool HasAuthorizationArgument(SpecificationCallerCall call) =>
        call.Syntax.ArgumentList.Arguments.Any(argument => IsAuthorizationArgument(argument.Expression, call.Model));

    static bool IsAuthorizationArgument(ExpressionSyntax expression, SemanticModel model) =>
        expression is TypeOfExpressionSyntax type ? IsAuthorizationType(model.GetTypeInfo(type.Type).Type) : IsAuthorizationType(model.GetTypeInfo(expression).Type);

    static bool IsAccessorFactory(IMethodSymbol? method) =>
        method?.ContainingType.Is("NSubstitute.Substitute") == true && HasAccessorTypeArgument(method);

    static bool HasAccessorTypeArgument(IMethodSymbol method) => method.TypeArguments is [var type] && type.Is(Accessor);

    static bool HasCurrentReceiver(SpecificationCallerCall call)
    {
        var current = CurrentReceiver(call);
        return current is not null && call.Model.GetTypeInfo(current.Expression).Type.Is(Accessor);
    }

    static bool IsAuthorizationType(ITypeSymbol? type) =>
        type is not null && HasAuthorizationContract(type);

    static bool HasAuthorizationContract(ITypeSymbol type) =>
        _authorizationTypes.Any(type.Is) || type.AllInterfaces.Any(implemented => _authorizationTypes.Any(implemented.Is));
}
