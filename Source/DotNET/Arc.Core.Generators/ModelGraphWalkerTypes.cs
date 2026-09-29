// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators;

/// <summary>
/// Finds the model types reachable from commands and query arguments, and the members the validation traversal walks
/// on each, so generated code can register them for walking without reflection.
/// </summary>
/// <remarks>
/// The traversal walks a value by its exact runtime type: a leaf stops it, a collection has its elements walked, and
/// any other type has its public instance properties that can be read without arguments walked, in the order
/// reflection returns them - the runtime type's own properties in declaration order, then each base type's, skipping
/// a base property that an already found property overrides or hides with the same signature. Generated walkers must
/// reproduce that exactly, so a type only gets one when every step can be decided from source:
/// <list type="bullet">
/// <item>the type is declared in the compilation, concrete, not a collection, and generated code can name it;</item>
/// <item>every base type declaring properties comes from the compilation or from an assembly that is not a reference
/// assembly - a reference assembly need not keep the declaration order or the non-public members that hide;</item>
/// <item>every walked property can be read by generated code as a plain value, without an obsolete, experimental or
/// trim/AOT annotated getter, and no property overrides with a covariant type.</item>
/// </list>
/// Any other type keeps the reflection walk. Types are followed through property types, collection element types,
/// nullable values and the type arguments of types declared elsewhere; a member typed as object, an interface or an
/// abstract type can hold any runtime type, so those are not followed.
/// </remarks>
internal static class ModelGraphWalkerTypes
{
    const string Registry = "global::Cratis.Arc.Validation.ModelGraphWalkers";
    const string Member = "global::Cratis.Arc.Validation.ModelGraphMember";

    /// <summary>
    /// The number of types one root's graph is followed through before stopping, bounding generation for large graphs.
    /// </summary>
    const int MaximumTypes = 1024;

    /// <summary>
    /// How deeply generic type arguments may nest, bounding generic types that expand themselves on every level.
    /// </summary>
    const int MaximumGenericDepth = 8;

    static readonly SymbolDisplayFormat _format = SymbolDisplayFormat.FullyQualifiedFormat;

    /// <summary>
    /// Gets the walkers for the model types reachable from the given types.
    /// </summary>
    /// <param name="roots">The types a traversal starts from, such as a command or a query argument.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <returns>The walkers to register.</returns>
    public static IEnumerable<ModelGraphWalker> For(IEnumerable<ITypeSymbol> roots, Compilation compilation)
    {
        var walkers = new List<ModelGraphWalker>();
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var pending = new Queue<INamedTypeSymbol>();
        foreach (var root in roots)
        {
            Follow(root, compilation, seen, pending);
        }

        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            var properties = WalkedProperties(type, compilation);
            if (properties is null)
            {
                continue;
            }

            foreach (var property in properties)
            {
                Follow(property.Type, compilation, seen, pending);
            }

            if (IsConcrete(type) && TypeNaming.CanBeNamed(type, compilation) && properties.TrueForAll(property => CanRead(property, compilation)))
            {
                walkers.Add(new(type.ToDisplayString(_format), Render(type, properties)));
            }
        }

        return walkers;
    }

    /// <summary>
    /// Follows a declared type to the types values of it can have at runtime that could get a walker.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <param name="seen">The types already followed.</param>
    /// <param name="pending">The types whose properties are still to be found.</param>
    static void Follow(ITypeSymbol type, Compilation compilation, HashSet<ITypeSymbol> seen, Queue<INamedTypeSymbol> pending)
    {
        if (seen.Count >= MaximumTypes || GenericDepth(type) > MaximumGenericDepth || !seen.Add(type))
        {
            return;
        }

        if (type is IArrayTypeSymbol array)
        {
            Follow(array.ElementType, compilation, seen, pending);
            return;
        }

        if (type is not INamedTypeSymbol named || named.TypeKind == TypeKind.Enum || named.IsRefLikeType)
        {
            return;
        }

        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            Follow(named.TypeArguments[0], compilation, seen, pending);
            return;
        }

        if (named.SpecialType != SpecialType.None)
        {
            return;
        }

        // The traversal walks the elements of any value that is enumerable instead of its properties.
        var enumerable = false;
        foreach (var @interface in named.AllInterfaces.Prepend(named))
        {
            if (@interface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                Follow(@interface.TypeArguments[0], compilation, seen, pending);
            }

            enumerable |= @interface.SpecialType == SpecialType.System_Collections_IEnumerable;
        }

        if (enumerable)
        {
            return;
        }

        if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition.ContainingAssembly, compilation.Assembly))
        {
            if (named.TypeKind is TypeKind.Class or TypeKind.Struct)
            {
                pending.Enqueue(named);
            }

            return;
        }

        foreach (var argument in named.TypeArguments)
        {
            Follow(argument, compilation, seen, pending);
        }
    }

    /// <summary>
    /// Gets the properties the traversal walks on a type, in the order reflection returns them.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <returns>The properties, or null when the order or the members cannot be decided from the compilation.</returns>
    static List<IPropertySymbol>? WalkedProperties(INamedTypeSymbol type, Compilation compilation)
    {
        // Reflection finds the properties of the type itself, then of each base type, where a private property of a
        // base type is never seen and a property already found hides one of the same name and signature or overrides
        // it. Hiding considers every property found so far, whatever its accessibility or whether it is static; only
        // then is the list narrowed to the readable public instance properties without index parameters.
        var found = new List<IPropertySymbol>();
        for (var current = type; current is not null; current = current.BaseType)
        {
            var declared = current.GetMembers().OfType<IPropertySymbol>()
                .Where(property => SymbolEqualityComparer.Default.Equals(current, type) || property.DeclaredAccessibility != Accessibility.Private)
                .ToArray();
            if (declared.Length > 0 && !KeepsDeclarations(current.ContainingAssembly, compilation))
            {
                return null;
            }

            foreach (var property in declared)
            {
                if (property.OverriddenProperty is { } overridden && !SymbolEqualityComparer.Default.Equals(property.Type, overridden.Type))
                {
                    return null;
                }

                if (!found.Exists(existing => Overrides(existing, property) || HasSameSignature(existing, property)))
                {
                    found.Add(property);
                }
            }
        }

        return [.. found.Where(property => property.DeclaredAccessibility == Accessibility.Public && !property.IsStatic &&
            property.Parameters.Length == 0 && property.GetMethod is not null)];
    }

    static bool KeepsDeclarations(IAssemblySymbol assembly, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly) ||
        !assembly.GetAttributes().Any(attribute => string.Equals(attribute.AttributeClass?.ToDisplayString(), "System.Runtime.CompilerServices.ReferenceAssemblyAttribute", StringComparison.Ordinal));

    static bool Overrides(IPropertySymbol property, IPropertySymbol candidate)
    {
        for (var overridden = property.OverriddenProperty; overridden is not null; overridden = overridden.OverriddenProperty)
        {
            if (SymbolEqualityComparer.Default.Equals(overridden, candidate))
            {
                return true;
            }
        }

        return false;
    }

    static bool HasSameSignature(IPropertySymbol property, IPropertySymbol candidate) =>
        string.Equals(property.MetadataName, candidate.MetadataName, StringComparison.Ordinal) &&
        property.IsStatic == candidate.IsStatic &&
        SymbolEqualityComparer.Default.Equals(property.Type, candidate.Type) &&
        property.Parameters.Length == candidate.Parameters.Length &&
        property.Parameters.Zip(candidate.Parameters, (left, right) => SymbolEqualityComparer.Default.Equals(left.Type, right.Type)).All(same => same);

    static bool IsConcrete(INamedTypeSymbol type) =>
        type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract && !type.IsStatic && !type.IsRefLikeType && !type.IsUnboundGenericType;

    /// <summary>
    /// Whether generated code can read a property as a plain value, and name its declaring and declared types.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <returns>Whether generated code can read the property.</returns>
    static bool CanRead(IPropertySymbol property, Compilation compilation) =>
        !property.ReturnsByRef && !property.ReturnsByRefReadonly &&
        property.Type is not (IPointerTypeSymbol or IFunctionPointerTypeSymbol) && !property.Type.IsRefLikeType &&
        SyntaxFacts.IsValidIdentifier(property.Name) &&
        compilation.IsSymbolAccessibleWithin(property, compilation.Assembly) &&
        compilation.IsSymbolAccessibleWithin(property.GetMethod!, compilation.Assembly) &&
        !IsFlagged(property) && !IsFlagged(property.GetMethod!) &&
        TypeNaming.CanBeNamed(property.ContainingType, compilation) &&
        (property.Type.TypeKind == TypeKind.Dynamic || TypeNaming.CanBeNamed(property.Type, compilation));

    /// <summary>
    /// Reading an obsolete, experimental or trim/AOT annotated member reports a diagnostic in the consumer's build that
    /// the reflection walk does not.
    /// </summary>
    /// <param name="symbol">The member.</param>
    /// <returns>Whether the member is flagged.</returns>
    static bool IsFlagged(ISymbol symbol) =>
        symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() is { } name &&
            (TypeNaming.IsFlag(name) ||
             string.Equals(name, "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute", StringComparison.Ordinal) ||
             string.Equals(name, "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute", StringComparison.Ordinal) ||
             string.Equals(name, "System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute", StringComparison.Ordinal) ||
             string.Equals(name, "System.Runtime.Versioning.RequiresPreviewFeaturesAttribute", StringComparison.Ordinal)));

    static int GenericDepth(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => GenericDepth(array.ElementType),
        INamedTypeSymbol { TypeArguments.Length: > 0 } named => 1 + named.TypeArguments.Max(GenericDepth),
        _ => 0
    };

    static string Render(INamedTypeSymbol type, List<IPropertySymbol> properties)
    {
        var name = type.ToDisplayString(_format);
        var source = new StringBuilder("        ").Append(Registry).Append(".Register(typeof(").Append(name).Append("), new ").Append(Member);
        if (properties.Count == 0)
        {
            return source.Append("[0]);").ToString();
        }

        source.AppendLine("[]").AppendLine("        {");
        foreach (var property in properties)
        {
            // The property is read through its declaring type: a type that hides it with a property of another type,
            // or with one reflection does not walk, would otherwise be read instead.
            var declaredType = property.Type.TypeKind == TypeKind.Dynamic ? "object" : property.Type.ToDisplayString(_format);
            source.Append("            new ").Append(Member).Append('(')
                .Append(SymbolDisplay.FormatLiteral(property.Name, true))
                .Append(", typeof(").Append(declaredType)
                .Append("), static instance => ((").Append(property.ContainingType.ToDisplayString(_format)).Append(")instance).@").Append(property.Name)
                .AppendLine("),");
        }

        return source.Append("        });").ToString();
    }
}
