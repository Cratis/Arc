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
            .Where(type => IsConcrete(type) && TypeNaming.CanBeNamed(type, compilation))
            .Select(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    static void Collect(ITypeSymbol type, List<ITypeSymbol> types, int depth, bool insideTuple = false)
    {
        if (depth > 8)
        {
            return;
        }

        if (type is not INamedTypeSymbol named)
        {
            types.Add(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
            return;
        }

        // A non-generic Task or ValueTask is awaited and never becomes a response.
        if (IsAwaitable(named, false))
        {
            return;
        }

        if (HasSingleWrappedValue(named))
        {
            Collect(named.TypeArguments[0], types, depth + 1, insideTuple);
            return;
        }

        if (TupleElements(named) is { IsDefault: false } elements)
        {
            // A nested tuple itself may be the response, while operation flattening can expose its elements instead.
            if (insideTuple)
            {
                types.Add(named.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
            }

            CollectEach(elements, types, depth + 1, true);
            return;
        }

        if (UnionCases(named) is { IsDefault: false } arms)
        {
            CollectEach(arms, types, depth + 1, insideTuple);
            return;
        }

        types.Add(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
    }

    static bool HasSingleWrappedValue(INamedTypeSymbol type) => type.IsGenericType &&
        (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T || IsAwaitable(type, true));

    static bool IsAwaitable(INamedTypeSymbol type, bool generic)
    {
        var definition = type.OriginalDefinition.ToDisplayString();
        var task = generic ? "System.Threading.Tasks.Task<TResult>" : "System.Threading.Tasks.Task";
        var valueTask = generic ? "System.Threading.Tasks.ValueTask<TResult>" : "System.Threading.Tasks.ValueTask";
        return string.Equals(definition, task, StringComparison.Ordinal) || string.Equals(definition, valueTask, StringComparison.Ordinal);
    }

    static void CollectEach(ImmutableArray<ITypeSymbol> elements, List<ITypeSymbol> types, int depth, bool insideTuple)
    {
        foreach (var element in elements)
        {
            Collect(element, types, depth, insideTuple);
        }
    }

    static ImmutableArray<ITypeSymbol> UnionCases(INamedTypeSymbol type)
    {
        // Use metadata names so the generator remains loadable by hosts without the .NET 11 contracts.
        if (type.AllInterfaces.Any(contract => contract.ToDisplayString() == "System.Runtime.CompilerServices.IUnion"))
        {
            return [.. type.InstanceConstructors
                .Where(IsUnionCaseConstructor)
                .Select(constructor => constructor.Parameters[0].Type)];
        }

        return OneOfArguments(type);
    }

    static bool IsUnionCaseConstructor(IMethodSymbol constructor) => constructor.DeclaredAccessibility == Accessibility.Public &&
        UnionCaseConvention.IsCaseConstructor(IsCompilerGenerated(constructor), constructor.Parameters.Length);

    static bool IsCompilerGenerated(IMethodSymbol constructor) => constructor.IsImplicitlyDeclared ||
        constructor.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == UnionCaseConvention.CompilerGeneratedAttributeName);

    /// <summary>
    /// Gets the element types of a value tuple or System.Tuple, which the pipeline both treats as ITuple.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The element types, or default when the type is not a tuple.</returns>
    static ImmutableArray<ITypeSymbol> TupleElements(INamedTypeSymbol type)
    {
        if (type.IsTupleType)
        {
            return [.. type.TupleElements.Select(element => element.Type)];
        }

        var definition = type.OriginalDefinition;
        if (!string.Equals(definition.ContainingNamespace?.ToDisplayString(), "System", StringComparison.Ordinal) ||
            !(string.Equals(definition.Name, "Tuple", StringComparison.Ordinal) || string.Equals(definition.Name, "ValueTuple", StringComparison.Ordinal)))
        {
            return default;
        }

        if (type.Arity == 0)
        {
            return [];
        }

        // A tuple of eight or more elements carries the remaining elements as a tuple in its last type argument,
        // which ITuple exposes as if they were elements of the same tuple.
        if (type.Arity == 8 && type.TypeArguments[7] is INamedTypeSymbol rest && TupleElements(rest) is { IsDefault: false } remaining)
        {
            return [.. type.TypeArguments.Take(7), .. remaining];
        }

        return type.TypeArguments;
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
}
