// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Recovers the member of an enumeration a constant names.
/// </summary>
/// <remarks>
/// The compiler hands a constant of an enumeration over as the number behind it, which is indistinguishable from any
/// other number by the time it reaches emission. The type it was written as is the only thing that still knows which
/// enumeration it belongs to, so the member is recovered here - at the one place holding both - and everything past
/// it carries a name.
/// </remarks>
public static class EnumConstants
{
    /// <summary>
    /// Determines whether a type is an enumeration.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is an enumeration.</returns>
    public static bool IsEnumeration(ITypeSymbol? type) => type is { TypeKind: TypeKind.Enum };

    /// <summary>
    /// Gets the enumeration an expression yields a value of.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <returns>The enumeration, or <see langword="null"/> when the expression yields something else.</returns>
    /// <remarks>
    /// An expression carries two types - what it is and what it was needed as - and either of them can be the one
    /// that knows about the enumeration. <c>UserRole.ClientContact</c> handed to a parameter taking anything is the
    /// first, and a number cast to <c>UserRole</c> is the second, so whichever of them names an enumeration is taken.
    /// </remarks>
    public static ITypeSymbol? EnumerationOf(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var type = semanticModel.GetTypeInfo(expression);

        return IsEnumeration(type.Type) ? type.Type : IsEnumeration(type.ConvertedType) ? type.ConvertedType : null;
    }

    /// <summary>
    /// Resolves the member of an enumeration a constant names.
    /// </summary>
    /// <param name="type">The type the constant was written as.</param>
    /// <param name="value">The value the compiler handed over, which for an enumeration is the number behind a member.</param>
    /// <param name="member">The member the value names.</param>
    /// <returns>True when the type is an enumeration declaring a member with that value.</returns>
    /// <remarks>
    /// A value no member is declared with - an arbitrary cast, or several flags combined into one - is not resolved.
    /// Inventing a name for it would describe a value the application does not have, so the caller is left to write
    /// the number and say what it lost.
    /// </remarks>
    public static bool TryResolve(ITypeSymbol? type, object? value, out EnumValue member)
    {
        member = null!;
        if (!IsEnumeration(type) || value is null || MemberOf(type!, value) is not { } name)
        {
            return false;
        }

        member = new(name);

        return true;
    }

    /// <summary>
    /// Gets the name of the member an enumeration declares a value with.
    /// </summary>
    /// <param name="type">The enumeration to read.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>The name, or <see langword="null"/> when no member is declared with the value.</returns>
    /// <remarks>
    /// Several members may share one value, and the order the compiler returns them in follows where they were read
    /// from rather than anything about the source. Ordering the candidates by name and taking the first is what makes
    /// the same compilation produce the same document, which is the whole point of a document worth committing.
    /// </remarks>
    static string? MemberOf(ITypeSymbol type, object value) =>
        type.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(_ => _.HasConstantValue && Equals(_.ConstantValue, value))
            .Select(_ => _.Name)
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
}
