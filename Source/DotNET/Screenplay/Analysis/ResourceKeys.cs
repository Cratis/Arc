// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the key a value is looked up in a resource by.
/// </summary>
/// <remarks>
/// An application that speaks more than one language declares its text once in a resource and names it from
/// everywhere it is shown. What it names is a property a build generates from the resource, whose whole body is a
/// lookup against the culture of the caller - so the compiler holds no value for it, and asking for one is how such
/// a value comes back indistinguishable from text assembled while the request runs.
/// <para>
/// The key is there to be read even though the text is not, and it is the better of the two to take: text would fix
/// one language into a document describing an application that has several. What identifies the property is the
/// shape of its getter rather than where it was declared or what it was called, because the file name and the naming
/// are conventions of one generator while the lookup is what makes it a resource at all.
/// </para>
/// </remarks>
public static class ResourceKeys
{
    /// <summary>
    /// The lookup a generated resource property resolves its text through.
    /// </summary>
    public const string GetString = "GetString";

    /// <summary>
    /// The member a generated resource property looks its text up in.
    /// </summary>
    public const string ResourceManager = "ResourceManager";

    /// <summary>
    /// Reads the key an expression is looked up by, qualified by the resource declaring it.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>The key, or <see langword="null"/> when the expression is no resource lookup.</returns>
    /// <remarks>
    /// The key is qualified because a key is unique to the resource declaring it and to nothing wider. A real
    /// application declares one resource per area of itself, and two of those areas naming the same rule the same
    /// way - an organization number required both of a customer and of a partner - is ordinary rather than a
    /// mistake. Bare, those two are one key that can carry only one text.
    /// </remarks>
    public static string? Read(ExpressionSyntax? expression, SemanticModel semanticModel)
    {
        if (expression is null ||
            semanticModel.GetSymbolInfo(expression).Symbol is not IPropertySymbol
            {
                IsStatic: true,
                Type.SpecialType: SpecialType.System_String,
                GetMethod: { } getter
            } property)
        {
            return null;
        }

        return LookedUpBy(getter) is { } key ? $"{property.ContainingType.Name}.{key}" : null;
    }

    /// <summary>
    /// Reads the key the getter of a property looks its value up by.
    /// </summary>
    /// <param name="getter">The getter to read.</param>
    /// <returns>The key, or <see langword="null"/> when the getter is no resource lookup.</returns>
    /// <remarks>
    /// A getter is asked for exactly one lookup. None at all is a property computing its value some other way, and
    /// more than one is a property choosing between them - a fallback, a composition - which is a value assembled
    /// while the request runs however each part of it was reached.
    /// <para>
    /// A property whose getter was compiled away into a referenced assembly has no body to read here and is left to
    /// be reported, because the key would then have to be guessed from the name the property carries - and that name
    /// is what a generator made of the key rather than the key itself.
    /// </para>
    /// </remarks>
    static string? LookedUpBy(IMethodSymbol getter)
    {
        if (getter.DeclaringSyntaxReferences.Length != 1)
        {
            return null;
        }

        var lookups = getter.DeclaringSyntaxReferences[0].GetSyntax()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsResourceLookup)
            .ToList();

        return lookups.Count == 1 ? KeyGivenTo(lookups[0]) : null;
    }

    /// <summary>
    /// Determines whether a call looks a value up in a resource.
    /// </summary>
    /// <param name="call">The call to check.</param>
    /// <returns>True when the call is a resource lookup.</returns>
    static bool IsResourceLookup(InvocationExpressionSyntax call) =>
        call.Expression is MemberAccessExpressionSyntax lookup &&
        string.Equals(lookup.Name.Identifier.ValueText, GetString, StringComparison.Ordinal) &&
        string.Equals(NameOf(lookup.Expression), ResourceManager, StringComparison.Ordinal);

    /// <summary>
    /// Gets the name an expression ends in.
    /// </summary>
    /// <param name="expression">The expression to name.</param>
    /// <returns>The name, or <see langword="null"/> when the expression names nothing.</returns>
    static string? NameOf(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        _ => null
    };

    /// <summary>
    /// Gets the key a lookup was given.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <returns>The key, or <see langword="null"/> when the lookup was given no key written down.</returns>
    static string? KeyGivenTo(InvocationExpressionSyntax call) =>
        call.ArgumentList.Arguments is [{ Expression: LiteralExpressionSyntax literal }, ..] &&
        literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;
}
