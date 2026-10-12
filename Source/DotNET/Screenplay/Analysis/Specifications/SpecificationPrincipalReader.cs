// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads literal principal constructors with one default identity and exact repeated claims.
/// </summary>
internal static class SpecificationPrincipalReader
{
    /// <summary>
    /// Recovers a caller from a supported principal constructor.
    /// </summary>
    /// <param name="expression">The principal construction.</param>
    /// <param name="model">The owning semantic model.</param>
    /// <returns>The exact caller or an unsupported-setup reason.</returns>
    internal static SpecificationCallerRecovery Read(ExpressionSyntax expression, SemanticModel model)
    {
        var principal = Creation(expression, model, "System.Security.Claims.ClaimsPrincipal");
        if (principal is null)
        {
            return SpecificationCallerRecovery.Failed("the caller principal is not a supported construction");
        }

        var arguments = ExplicitArguments(principal);
        if (arguments.Count == 0)
        {
            return new(new(false, [], []), null);
        }

        if (!HasSingleIdentity(principal))
        {
            return SpecificationCallerRecovery.Failed("the caller must have exactly one identity");
        }

        return ReadIdentity(arguments[0].Expression, model);
    }

    static bool HasSingleIdentity(IObjectCreationOperation principal) =>
        principal.Constructor?.Parameters is [{ Type: { } type }] && type.Is("System.Security.Principal.IIdentity");

    static SpecificationCallerRecovery ReadIdentity(ExpressionSyntax expression, SemanticModel model)
    {
        var identity = Creation(expression, model, "System.Security.Claims.ClaimsIdentity");
        if (identity is null)
        {
            return SpecificationCallerRecovery.Failed("the caller identity is not a supported construction");
        }

        var arguments = ExplicitArguments(identity);
        if (identity.Constructor?.Parameters.Length == 0)
        {
            return new(new(false, [], []), null);
        }

        if (!HasDefaultClaimTypes(identity))
        {
            return SpecificationCallerRecovery.Failed("the caller identity must use the default name and role claim types");
        }

        var authentication = arguments.SingleOrDefault(argument => argument.Name == "authenticationType");
        var constant = authentication is null ? default : model.GetConstantValue(authentication.Expression);
        if (!constant.HasValue)
        {
            return SpecificationCallerRecovery.Failed("the caller authentication type is not a compile-time constant");
        }

        var authenticated = constant.Value is string { Length: > 0 };
        var claims = arguments.SingleOrDefault(argument => argument.Name == "claims");
        return ReadClaims(claims?.Expression, authenticated, model);
    }

    static bool HasDefaultClaimTypes(IObjectCreationOperation identity) =>
        identity.Constructor?.Parameters.Select(parameter => parameter.Name).SequenceEqual(["claims", "authenticationType"], StringComparer.Ordinal) == true;

    static SpecificationCallerRecovery ReadClaims(ExpressionSyntax? expression, bool authenticated, SemanticModel model)
    {
        var elements = Elements(expression, model);
        if (elements is null)
        {
            return SpecificationCallerRecovery.Failed("the caller claims are not a literal collection, array, or List<Claim> initializer");
        }

        var claims = elements.ConvertAll(element => Claim(element, model));
        if (claims.Contains(null))
        {
            return SpecificationCallerRecovery.Failed("every caller claim must use the two-string constructor with non-null compile-time constant type and value");
        }

        if (!authenticated && claims.Count > 0)
        {
            return SpecificationCallerRecovery.Failed("an unauthenticated caller holding claims is replaced by Arc's anonymous principal");
        }

        var roles = claims.OfType<KeyValuePair<string, string>>().Where(IsRole).Select(claim => claim.Value).ToList();
        if (roles.Exists(role => role.Length == 0))
        {
            return SpecificationCallerRecovery.Failed("the caller has an empty role value");
        }

        return new(new(authenticated, roles, claims.OfType<KeyValuePair<string, string>>().Where(claim => !IsRole(claim)).ToList()), null);
    }

    static bool IsRole(KeyValuePair<string, string> claim) => string.Equals(claim.Key, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase);

    static KeyValuePair<string, string>? Claim(ExpressionSyntax expression, SemanticModel model)
    {
        var claim = Creation(expression, model, "System.Security.Claims.Claim");
        if (claim?.Constructor?.Parameters.Length != 2)
        {
            return null;
        }

        var arguments = ExplicitArguments(claim);
        var type = arguments.SingleOrDefault(argument => argument.Name == "type");
        var value = arguments.SingleOrDefault(argument => argument.Name == "value");
        if (type is null || value is null)
        {
            return null;
        }

        return model.GetConstantValue(type.Expression).Value is string claimType && model.GetConstantValue(value.Expression).Value is string claimValue
            ? new(claimType, claimValue) : null;
    }

    static List<ExpressionSyntax>? Elements(ExpressionSyntax? expression, SemanticModel model) => expression switch
    {
        CollectionExpressionSyntax collection => collection.Elements.All(element => element is ExpressionElementSyntax)
            ? collection.Elements.OfType<ExpressionElementSyntax>().Select(element => element.Expression).ToList() : null,
        ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions.ToList(),
        ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions.ToList(),
        ObjectCreationExpressionSyntax { Initializer: { } initializer } creation =>
            IsEmptyClaimList(creation, model) ? initializer.Expressions.ToList() : null,
        _ => null
    };

    static bool IsEmptyClaimList(ObjectCreationExpressionSyntax creation, SemanticModel model) =>
        creation.ArgumentList is null or { Arguments.Count: 0 } && IsClaimList(creation, model);

    static bool IsClaimList(ObjectCreationExpressionSyntax creation, SemanticModel model) =>
        model.GetTypeInfo(creation).Type is INamedTypeSymbol { TypeArguments: [var element] } type &&
        IsClaimListType(type, element);

    static bool IsClaimListType(INamedTypeSymbol type, ITypeSymbol element) =>
        type.Is("System.Collections.Generic.List`1") && element.Is("System.Security.Claims.Claim");

    static IObjectCreationOperation? Creation(ExpressionSyntax expression, SemanticModel model, string type)
    {
        var creation = model.GetOperation(expression) as IObjectCreationOperation;
        return IsPlainConstruction(creation, type) ? creation : null;
    }

    static bool IsPlainConstruction(IObjectCreationOperation? creation, string type) =>
        creation?.Type.Is(type) == true && creation.Initializer is null;

    static List<NamedArgument> ExplicitArguments(IObjectCreationOperation creation) =>
        creation.Arguments.Where(argument => !argument.IsImplicit).Select(argument =>
            new NamedArgument(argument.Parameter!.Name, ((ArgumentSyntax)argument.Syntax).Expression)).ToList();

    sealed record NamedArgument(string Name, ExpressionSyntax Expression);
}
