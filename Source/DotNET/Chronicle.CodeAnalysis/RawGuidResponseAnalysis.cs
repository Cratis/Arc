// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Bounded, signature-based runtime parity checks for ARCCHR0010, independent of ARCCHR0002's candidate convention.
/// </summary>
static class RawGuidResponseAnalysis
{
    const string KeyAttribute = "Cratis.Chronicle.Keys.KeyAttribute";

    /// <summary>
    /// Checks the same property and provider eligibility used by EventSourceValuesProvider.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>Whether the runtime has a declared event source id.</returns>
    internal static bool HasRuntimeEventSourceId(INamedTypeSymbol command, Compilation compilation) =>
        command.AllInterfaces.Any(type => IsType(type, "Cratis.Chronicle.Events.ICanProvideEventSourceId", compilation)) ||
        PublicProperties(command).Any(property =>
            IsEventSourceId(property.Type, compilation) ||
            HasKeyAttribute(property, compilation) ||
            property.ContainingType.InstanceConstructors.SelectMany(constructor => constructor.Parameters).Any(parameter =>
                parameter.Name == property.Name &&
                SymbolEqualityComparer.Default.Equals(parameter.Type, property.Type) &&
                HasAttribute(parameter, KeyAttribute, compilation)));

    /// <summary>
    /// Finds public instance handlers, including inherited handlers not hidden by a derived declaration.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <returns>The handler methods.</returns>
    internal static IEnumerable<IMethodSymbol> HandleMethods(INamedTypeSymbol command)
    {
        var seen = new List<IMethodSymbol>();
        for (var type = command; type is not null; type = type.BaseType)
        {
            foreach (var method in type.GetMembers("Handle").OfType<IMethodSymbol>().Where(IsPublicHandler))
            {
                if (!seen.Exists(derived => HidesHandler(derived, method)))
                {
                    seen.Add(method);
                    yield return method;
                }
            }
        }
    }

    /// <summary>
    /// Finds bare event branches beside a raw Guid in supported response signatures.
    /// </summary>
    /// <param name="returnType">The handler return type.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>The statically identifiable event types that use the fallback target.</returns>
    internal static IEnumerable<ITypeSymbol> UntargetedEventsBesideGuid(ITypeSymbol returnType, Compilation compilation) =>
        UntargetedEventsBesideGuid(returnType, compilation, new ResponseTypeTraversal());

    /// <summary>
    /// Unwraps the single Task or ValueTask awaited by the model-bound handler.
    /// </summary>
    /// <param name="type">The declared return type.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>The awaited type, or the original type.</returns>
    internal static ITypeSymbol UnwrapAwaitable(ITypeSymbol type, Compilation compilation) =>
        type is INamedTypeSymbol awaitable &&
        (IsType(awaitable, "System.Threading.Tasks.Task`1", compilation) || IsType(awaitable, "System.Threading.Tasks.ValueTask`1", compilation))
            ? awaitable.TypeArguments[0]
            : type;

    /// <summary>
    /// Checks the event metadata recognized by Chronicle, including inherited attributes.
    /// </summary>
    /// <param name="type">The candidate event type.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>Whether the type has event metadata.</returns>
    internal static bool HasEventTypeAttribute(ITypeSymbol type, Compilation compilation)
    {
        // EventTypeAttribute is inherited; Chronicle recognizes its metadata through the event's base types.
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (HasAttribute(current, "Cratis.Chronicle.Events.EventTypeAttribute", compilation))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds alternatives of native unions and the known Result and OneOf wrappers consumed by the pipeline.
    /// </summary>
    /// <param name="type">The response type.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>The unwrapped branch types.</returns>
    internal static IEnumerable<ITypeSymbol> UnionBranches(ITypeSymbol type, Compilation compilation) =>
        UnionBranches(type, compilation, new ResponseTypeTraversal());

    /// <summary>
    /// Gets immediate alternatives, or a default array for a nonunion response.
    /// </summary>
    /// <param name="type">The unwrapped response type.</param>
    /// <param name="compilation">The compilation containing wrapper identities.</param>
    /// <returns>The declared alternatives without recursively expanding them.</returns>
    internal static ImmutableArray<ITypeSymbol> UnionAlternatives(ITypeSymbol type, Compilation compilation)
    {
        var cases = NativeUnionSymbols.Cases(type);
        return !cases.IsDefault ? cases : KnownUnionAlternatives(type, compilation);
    }

    /// <summary>
    /// Finds exact concurrency scopes in supported union, tuple, and awaitable responses.
    /// </summary>
    /// <param name="type">The response type.</param>
    /// <param name="compilation">The compilation containing scope identities.</param>
    /// <returns>Whether any branch declares exact scopes.</returns>
    internal static bool HasExactScopes(ITypeSymbol type, Compilation compilation) =>
        HasExactScopes(type, compilation, new ResponseTypeTraversal());

    /// <summary>
    /// Checks an attribute by its framework identity, not its short name.
    /// </summary>
    /// <param name="symbol">The attributed symbol.</param>
    /// <param name="metadataName">The attribute's metadata name.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>Whether the attribute is present.</returns>
    internal static bool HasAttribute(ISymbol symbol, string metadataName, Compilation compilation) =>
        symbol.GetAttributes().Any(attribute => IsType(attribute.AttributeClass, metadataName, compilation));

    /// <summary>
    /// Checks a type's identity against a framework metadata name, ignoring generic arguments.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="metadataName">The metadata name to compare with.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>Whether the type is the named type.</returns>
    internal static bool IsType(ITypeSymbol? type, string metadataName, Compilation compilation) =>
        type is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, compilation.GetTypeByMetadataName(metadataName));

    /// <summary>
    /// Finds the public properties reflection would report for the command, honoring hiding and overriding.
    /// </summary>
    /// <param name="command">The command type.</param>
    /// <returns>The visible public properties.</returns>
    internal static IEnumerable<IPropertySymbol> PublicProperties(INamedTypeSymbol command)
    {
        var seen = new List<IPropertySymbol>();
        for (var type = command; type is not null; type = type.BaseType)
        {
            // Type.GetProperties(): public instance properties, plus public static properties declared on this type.
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property => IsPropertyVisible(property, type, command)))
            {
                // Reflection hides by name and signature (including property type), not just by name. A private
                // property on the reflected type hides a base property, but private inherited properties are skipped.
                if (seen.Exists(derived => HidesProperty(derived, property)))
                {
                    continue;
                }
                seen.Add(property);
                if (property.DeclaredAccessibility == Accessibility.Public)
                {
                    yield return property;
                }
            }
        }
    }

    static bool IsPublicHandler(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public && !method.IsStatic;

    static bool HidesHandler(IMethodSymbol derived, IMethodSymbol method) =>
        SameParameters(derived.Parameters, method.Parameters) && derived.Arity == method.Arity;

    static bool IsPropertyVisible(IPropertySymbol property, INamedTypeSymbol declaringType, INamedTypeSymbol command) =>
        SymbolEqualityComparer.Default.Equals(declaringType, command) || (!property.IsStatic && property.DeclaredAccessibility != Accessibility.Private);

    static bool HidesProperty(IPropertySymbol derived, IPropertySymbol property) =>
        Overrides(derived, property) || SamePropertySignature(derived, property);

    static bool SamePropertySignature(IPropertySymbol left, IPropertySymbol right) =>
        left.MetadataName == right.MetadataName && SymbolEqualityComparer.Default.Equals(left.Type, right.Type) &&
        SameParameters(left.Parameters, right.Parameters);

    static ITypeSymbol[] UnionBranches(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal) =>
        traversal.Visit(type, () => ExpandedBranches(type, compilation, traversal), []);

    static ITypeSymbol[] ExpandedBranches(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal)
    {
        type = UnwrapAwaitable(type, compilation);
        var alternatives = UnionAlternatives(type, compilation);
        return alternatives.IsDefault ? [type] :
            alternatives.SelectMany(alternative => UnionBranches(alternative, compilation, traversal)).ToArray();
    }

    static ImmutableArray<ITypeSymbol> KnownUnionAlternatives(ITypeSymbol type, Compilation compilation) =>
        type is INamedTypeSymbol named && IsKnownUnion(named, compilation) ? named.TypeArguments : default;

    static bool IsKnownUnion(INamedTypeSymbol type, Compilation compilation) =>
        IsKnownUnionDefinition(type, compilation) &&
        type.AllInterfaces.Any(contract => IsType(contract, "OneOf.IOneOf", compilation));

    static bool IsKnownUnionDefinition(INamedTypeSymbol type, Compilation compilation) =>
        IsType(type, $"OneOf.OneOf`{type.Arity}", compilation) || IsType(type, "Cratis.Monads.Result`2", compilation);

    static bool HasExactScopes(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal) =>
        traversal.Visit(type, () => BranchHasExactScopes(type, compilation, traversal), false);

    static bool BranchHasExactScopes(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal)
    {
        type = UnwrapAwaitable(type, compilation);
        if (IsType(type, "Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes", compilation))
        {
            return true;
        }
        if (type is INamedTypeSymbol { IsTupleType: true } tuple)
        {
            return tuple.TupleElements.Any(element => HasExactScopes(element.Type, compilation, traversal));
        }
        var alternatives = UnionAlternatives(type, compilation);
        return !alternatives.IsDefault && alternatives.Any(alternative => HasExactScopes(alternative, compilation, traversal));
    }

    static bool IsGuid(ITypeSymbol? type, Compilation compilation) => IsType(type, "System.Guid", compilation);

    static bool Overrides(IPropertySymbol derived, IPropertySymbol property)
    {
        for (var current = derived.OverriddenProperty; current is not null; current = current.OverriddenProperty)
        {
            if (SymbolEqualityComparer.Default.Equals(current, property))
            {
                return true;
            }
        }

        return false;
    }

    static bool SameParameters(IEnumerable<IParameterSymbol> left, IEnumerable<IParameterSymbol> right) =>
        left.Select(parameter => parameter.Type).SequenceEqual(right.Select(parameter => parameter.Type), SymbolEqualityComparer.Default);

    static bool HasKeyAttribute(IPropertySymbol property, Compilation compilation)
    {
        // Attribute.IsDefined(PropertyInfo, KeyAttribute) inherits attributes through overrides, not hidden properties.
        for (var current = property; current is not null; current = current.OverriddenProperty)
        {
            if (HasAttribute(current, KeyAttribute, compilation))
            {
                return true;
            }
        }

        return false;
    }

    static ITypeSymbol[] UntargetedEventsBesideGuid(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal) =>
        traversal.Visit(type, () => UntargetedBranchEvents(type, compilation, traversal).ToArray(), []);

    static IEnumerable<ITypeSymbol> UntargetedBranchEvents(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal)
    {
        type = UnwrapAwaitable(type, compilation);
        var alternatives = UnionAlternatives(type, compilation);
        if (!alternatives.IsDefault)
        {
            return alternatives.SelectMany(alternative => UntargetedEventsBesideGuid(alternative, compilation, traversal));
        }
        return type is INamedTypeSymbol { IsTupleType: true } tuple ? TupleEventsBesideGuid(tuple, compilation, traversal) : [];
    }

    static IEnumerable<ITypeSymbol> TupleEventsBesideGuid(INamedTypeSymbol tuple, Compilation compilation, ResponseTypeTraversal traversal)
    {
        if (tuple.TupleElements.Any(element => IsGuid(element.Type, compilation)) &&
            !tuple.TupleElements.Any(element => IsEventSourceId(element.Type, compilation)))
        {
            // Explicit wrappers target only their own events, not bare events in sibling tuple elements.
            return tuple.TupleElements.SelectMany(element => BareEvents(element.Type, compilation, traversal));
        }

        return tuple.TupleElements.SelectMany(element => UntargetedEventsBesideGuid(element.Type, compilation, traversal));
    }

    static ITypeSymbol[] BareEvents(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal) =>
        traversal.Visit(type, () => BareBranchEvents(type, compilation, traversal).ToArray(), [], inheritedContext: true);

    static IEnumerable<ITypeSymbol> BareBranchEvents(ITypeSymbol type, Compilation compilation, ResponseTypeTraversal traversal)
    {
        type = UnwrapAwaitable(type, compilation);
        var alternatives = UnionAlternatives(type, compilation);
        if (!alternatives.IsDefault)
        {
            return alternatives.SelectMany(alternative => BareEvents(alternative, compilation, traversal));
        }
        if (IsType(type, "Cratis.Chronicle.EventSequences.EventForEventSourceId", compilation))
        {
            return [];
        }
        if (HasEventTypeAttribute(type, compilation))
        {
            return [type];
        }
        return type is INamedTypeSymbol { IsTupleType: true } tuple
            ? tuple.TupleElements.SelectMany(element => BareEvents(element.Type, compilation, traversal))
            : CollectionEvents(type, compilation);
    }

    static IEnumerable<ITypeSymbol> CollectionEvents(ITypeSymbol type, Compilation compilation)
    {
        // EventsCommandResponseValueHandler requires IEnumerable<object>; value-type collections are not covariant.
        // Erased IEnumerable<object> payloads and arbitrary factories cannot be proven from their signatures.
        var element = type is IArrayTypeSymbol { Rank: 1 } array ? array.ElementType : EnumerableElement(type, compilation);
        return element is { IsReferenceType: true } && HasEventTypeAttribute(element, compilation) ? [element] : [];
    }

    static ITypeSymbol? EnumerableElement(ITypeSymbol type, Compilation compilation) =>
        type.AllInterfaces.Prepend(type).OfType<INamedTypeSymbol>()
            .FirstOrDefault(candidate => IsType(candidate, "System.Collections.Generic.IEnumerable`1", compilation))?.TypeArguments[0];

    static bool IsEventSourceId(ITypeSymbol type, Compilation compilation)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (IsType(current, "Cratis.Chronicle.Events.EventSourceId", compilation) ||
                IsType(current, "Cratis.Chronicle.Events.EventSourceId`1", compilation))
            {
                return true;
            }
        }

        return false;
    }
}
