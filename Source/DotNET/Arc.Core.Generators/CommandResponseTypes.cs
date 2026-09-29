// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators;

/// <summary>
/// Finds the runtime types a model-bound command's Handle methods can produce as a response the command pipeline wraps
/// in a CommandResult of that type.
/// </summary>
/// <remarks>
/// The pipeline wraps a response by its runtime type after awaiting the Handle result and taking values out of tuples
/// and OneOf values. Only types a runtime value can have exactly - not interfaces, abstract types or object - and that
/// generated code in the compilation can name are returned; any other response keeps the reflection path.
/// </remarks>
internal static class CommandResponseTypes
{
    /// <summary>
    /// Gets the fully qualified names of the response types a command can produce.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <returns>Fully qualified type names.</returns>
    public static IEnumerable<string> For(INamedTypeSymbol command, Compilation compilation)
    {
        var types = new List<ITypeSymbol>();
        foreach (var handle in CommandOperationConvention.Methods(command, "Handle").Where(method => !method.IsStatic && !method.ReturnsVoid))
        {
            Collect(handle.ReturnType, types, 0);
        }

        return types
            .Where(type => IsConcrete(type) && CanBeNamed(type, compilation))
            .Select(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    static void Collect(ITypeSymbol type, List<ITypeSymbol> types, int depth)
    {
        if (depth > 8)
        {
            return;
        }

        if (type is INamedTypeSymbol named)
        {
            var definition = named.OriginalDefinition.ToDisplayString();
            if (named.IsGenericType && (definition == "System.Threading.Tasks.Task<TResult>" || definition == "System.Threading.Tasks.ValueTask<TResult>" ||
                named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T))
            {
                Collect(named.TypeArguments[0], types, depth + 1);
                return;
            }

            if (named.IsTupleType)
            {
                foreach (var element in named.TupleElements)
                {
                    Collect(element.Type, types, depth + 1);
                }

                return;
            }

            if (OneOfArguments(named) is { IsDefault: false } arms)
            {
                foreach (var arm in arms)
                {
                    Collect(arm, types, depth + 1);
                }

                return;
            }
        }

        types.Add(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
    }

    static ImmutableArray<ITypeSymbol> OneOfArguments(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            if (current.IsGenericType && string.Equals(definition.ContainingNamespace?.ToDisplayString(), "OneOf", StringComparison.Ordinal) &&
                (string.Equals(definition.Name, "OneOf", StringComparison.Ordinal) || string.Equals(definition.Name, "OneOfBase", StringComparison.Ordinal)))
            {
                return current.TypeArguments;
            }
        }

        return default;
    }

    static bool IsConcrete(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol => true,
        INamedTypeSymbol named => named.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Enum or TypeKind.Delegate &&
            !named.IsAbstract && !named.IsStatic && !named.IsRefLikeType && !named.IsUnboundGenericType &&
            named.SpecialType is not (SpecialType.System_Object or SpecialType.System_Void),
        _ => false
    };

    static bool CanBeNamed(ITypeSymbol type, Compilation compilation) => type switch
    {
        IArrayTypeSymbol array => CanBeNamed(array.ElementType, compilation),
        INamedTypeSymbol named => !IsFileLocalOrFlagged(named) && compilation.IsSymbolAccessibleWithin(named, compilation.Assembly) &&
            named.TypeArguments.All(argument => CanBeNamed(argument, compilation)),
        _ => false
    };

    /// <summary>
    /// Naming a file-local type from generated code fails, and naming an obsolete or experimental type reports a
    /// warning or error in the consumer's build.
    /// </summary>
    /// <param name="type">The type to check, with its containing types.</param>
    /// <returns>Whether generated code must not name the type.</returns>
    static bool IsFileLocalOrFlagged(INamedTypeSymbol type) =>
        type.IsFileLocal ||
        type.GetAttributes().Any(attribute => IsFlag(attribute.AttributeClass?.ToDisplayString())) ||
        (type.ContainingType is not null && IsFileLocalOrFlagged(type.ContainingType));

    static bool IsFlag(string? attribute) =>
        string.Equals(attribute, "System.ObsoleteAttribute", StringComparison.Ordinal) ||
        string.Equals(attribute, "System.Diagnostics.CodeAnalysis.ExperimentalAttribute", StringComparison.Ordinal);
}
