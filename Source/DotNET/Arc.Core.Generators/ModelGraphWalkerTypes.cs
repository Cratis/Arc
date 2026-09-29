// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Generators;

/// <summary>
/// Finds the model types reachable from commands and query arguments, and the members the validation traversal walks
/// on each, so generated code can register them for walking without reflection.
/// </summary>
/// <remarks>
/// The traversal walks a value by its exact runtime type: a leaf stops it, a collection has its elements walked, and
/// any other type has its public instance properties that can be read without arguments walked, in the order
/// reflection returns them. A registered walker replaces that reflection walk for its exact type, so any difference
/// would silently change what gets validated. Walkers are therefore only generated where they are plainly the same
/// as the reflection walk, and every other type keeps reflection:
/// <list type="bullet">
/// <item>the type is declared in the compilation, concrete, not a collection, and generated code can name it;</item>
/// <item>the type and every base type other than <see cref="object"/> and <see cref="ValueType"/> are declared in
/// source in the compilation, so the properties and their order are all known - a base type from any other assembly
/// may have members the compiler does not import, and its members can change after this compilation is built;</item>
/// <item>none of those types has a partial declaration, since another source generator can add properties to it
/// that this generator does not see;</item>
/// <item>no two properties across the type and its base types share a name when one of them is public, so no
/// property overrides or hides another the traversal walks - which property reflection keeps then depends on raw
/// metadata signatures that symbols do not reproduce exactly;</item>
/// <item>every walked property can be read by generated code as a plain value, without an obsolete, experimental or
/// trim/AOT annotated property or getter.</item>
/// </list>
/// Types are followed through property types, collection element types, nullable values and the type arguments of
/// types declared elsewhere, including through types that get no walker, since the traversal reaches their members by
/// reflection. A member typed as object, an interface or an abstract type can hold any runtime type, so those are not
/// followed.
/// </remarks>
internal static class ModelGraphWalkerTypes
{
    const string Registry = "global::Cratis.Arc.Validation.ModelGraphWalkers";
    const string Member = "global::Cratis.Arc.Validation.ModelGraphMember";

    /// <summary>
    /// The number of types followed across all roots of a compilation before stopping, bounding generation for large
    /// graphs.
    /// </summary>
    const int MaximumTypes = 4096;

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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The walkers to register.</returns>
    public static IEnumerable<ModelGraphWalker> For(IEnumerable<ITypeSymbol> roots, Compilation compilation, CancellationToken cancellationToken)
    {
        var walkers = new List<ModelGraphWalker>();
        if (IsExperimental(compilation.Assembly) || IsExperimental(compilation.SourceModule))
        {
            return walkers;
        }

        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var pending = new Queue<INamedTypeSymbol>();
        foreach (var root in roots)
        {
            Follow(root, compilation, seen, pending);
        }

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var type = pending.Dequeue();
            for (var current = type; current is not null; current = current.BaseType)
            {
                foreach (var property in current.GetMembers().OfType<IPropertySymbol>().Where(IsWalked))
                {
                    Follow(property.Type, compilation, seen, pending);
                }
            }

            if (WalkedProperties(type, compilation, cancellationToken) is { } properties)
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

        if (IsFromCompilation(named, compilation))
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
    /// Gets the properties the traversal walks on a type, in the order reflection returns them, when a walker for the
    /// type is plainly the same as the reflection walk.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The properties, or null when the type keeps the reflection walk.</returns>
    static List<IPropertySymbol>? WalkedProperties(INamedTypeSymbol type, Compilation compilation, CancellationToken cancellationToken)
    {
        if (!IsConcrete(type) || !TypeNaming.CanBeNamed(type, compilation))
        {
            return null;
        }

        // Reflection finds the properties of the type itself, then of each base type, where a property already found
        // hides or overrides one of the same name and signature. With no public property sharing its name with any
        // other, nothing the traversal walks is hidden or overridden, and each type's properties follow in
        // declaration order.
        var declared = new List<IPropertySymbol>();
        for (var current = type; current is not null && !IsRootType(current); current = current.BaseType)
        {
            if (!IsFromCompilation(current, compilation) || current.OriginalDefinition.DeclaringSyntaxReferences.Length == 0 || IsPartial(current.OriginalDefinition, cancellationToken))
            {
                return null;
            }

            declared.AddRange(current.GetMembers().OfType<IPropertySymbol>());
        }

        var sharesName = declared
            .GroupBy(property => property.MetadataName, StringComparer.Ordinal)
            .Any(group => group.Skip(1).Any() && group.Any(property => property.DeclaredAccessibility == Accessibility.Public));
        if (sharesName)
        {
            return null;
        }

        var walked = declared.Where(IsWalked).ToList();
        return walked.TrueForAll(property => CanRead(property, compilation)) ? walked : null;
    }

    /// <summary>
    /// Whether the traversal walks a property: a public instance property without index parameters that has a getter.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>Whether the property is walked.</returns>
    static bool IsWalked(IPropertySymbol property) =>
        property.DeclaredAccessibility == Accessibility.Public && !property.IsStatic && property.Parameters.Length == 0 && property.GetMethod is not null;

    static bool IsRootType(INamedTypeSymbol type) =>
        type.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType;

    static bool IsFromCompilation(INamedTypeSymbol type, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(type.OriginalDefinition.ContainingAssembly, compilation.Assembly);

    static bool IsPartial(INamedTypeSymbol type, CancellationToken cancellationToken) =>
        type.DeclaringSyntaxReferences.Length > 1 ||
        type.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax(cancellationToken) is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

    static bool IsConcrete(INamedTypeSymbol type) =>
        type.TypeKind is TypeKind.Class or TypeKind.Struct && !type.IsAbstract && !type.IsStatic && !type.IsRefLikeType && !type.IsUnboundGenericType;

    /// <summary>
    /// Whether generated code can read a property as a plain value through its declaring type.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <returns>Whether generated code can read the property.</returns>
    static bool CanRead(IPropertySymbol property, Compilation compilation) =>
        !property.IsOverride && !property.ReturnsByRef && !property.ReturnsByRefReadonly &&
        property.Type is not (IPointerTypeSymbol or IFunctionPointerTypeSymbol) && !property.Type.IsRefLikeType &&
        SyntaxFacts.IsValidIdentifier(property.Name) &&
        compilation.IsSymbolAccessibleWithin(property, compilation.Assembly) &&
        compilation.IsSymbolAccessibleWithin(property.GetMethod!, compilation.Assembly) &&
        !IsFlagged(property) && !IsFlagged(property.GetMethod!) &&
        TypeNaming.CanBeNamed(property.ContainingType, compilation);

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

    /// <summary>
    /// An experimental assembly or module makes every type in it experimental, so generated code naming them would
    /// report a diagnostic.
    /// </summary>
    /// <param name="symbol">The assembly or module.</param>
    /// <returns>Whether it is experimental.</returns>
    static bool IsExperimental(ISymbol symbol) =>
        symbol.GetAttributes().Any(attribute => string.Equals(attribute.AttributeClass?.ToDisplayString(), "System.Diagnostics.CodeAnalysis.ExperimentalAttribute", StringComparison.Ordinal));

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
            source.Append("            new ").Append(Member).Append('(')
                .Append(SymbolDisplay.FormatLiteral(property.Name, true))
                .Append(", static instance => ((").Append(property.ContainingType.ToDisplayString(_format)).Append(")instance).@").Append(property.Name)
                .AppendLine("),");
        }

        return source.Append("        });").ToString();
    }
}
