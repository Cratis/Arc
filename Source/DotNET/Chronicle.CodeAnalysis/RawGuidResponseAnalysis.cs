// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            foreach (var method in type.GetMembers("Handle").OfType<IMethodSymbol>().Where(method =>
                method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public && !method.IsStatic))
            {
                if (!seen.Exists(derived => SameParameters(derived.Parameters, method.Parameters) && derived.Arity == method.Arity))
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
    internal static IEnumerable<ITypeSymbol> UntargetedEventsBesideGuid(ITypeSymbol returnType, Compilation compilation)
    {
        // ModelBoundCommandHandler awaits Task/ValueTask once. The pipeline then unwraps IOneOf branches.
        if (returnType is INamedTypeSymbol awaitable &&
            (IsType(awaitable, "System.Threading.Tasks.Task`1", compilation) || IsType(awaitable, "System.Threading.Tasks.ValueTask`1", compilation)))
        {
            returnType = awaitable.TypeArguments[0];
        }

        foreach (var branch in UnionBranches(returnType, compilation))
        {
            if (branch is not INamedTypeSymbol { IsTupleType: true } tuple ||
                !tuple.TupleElements.Any(element => IsGuid(element.Type, compilation)) ||
                tuple.TupleElements.Any(element => IsEventSourceId(element.Type, compilation)))
            {
                continue;
            }

            // Explicit wrappers target only their own events, not bare events in sibling tuple elements.
            foreach (var eventType in tuple.TupleElements.SelectMany(element => BareEvents(element.Type, compilation)))
            {
                yield return eventType;
            }
        }
    }

    static bool IsGuid(ITypeSymbol? type, Compilation compilation) => IsType(type, "System.Guid", compilation);

    static IEnumerable<IPropertySymbol> PublicProperties(INamedTypeSymbol command)
    {
        var seen = new List<IPropertySymbol>();
        for (var type = command; type is not null; type = type.BaseType)
        {
            // Type.GetProperties(): public instance properties, plus public static properties declared on this type.
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property =>
                SymbolEqualityComparer.Default.Equals(type, command) || (!property.IsStatic && property.DeclaredAccessibility != Accessibility.Private)))
            {
                // Reflection hides by name and signature (including property type), not just by name. A private
                // property on the reflected type hides a base property, but private inherited properties are skipped.
                if (!seen.Exists(derived => Overrides(derived, property) ||
                    (derived.MetadataName == property.MetadataName &&
                     SymbolEqualityComparer.Default.Equals(derived.Type, property.Type) &&
                     SameParameters(derived.Parameters, property.Parameters))))
                {
                    seen.Add(property);
                    if (property.DeclaredAccessibility == Accessibility.Public)
                    {
                        yield return property;
                    }
                }
            }
        }
    }

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

    static IEnumerable<ITypeSymbol> BareEvents(ITypeSymbol type, Compilation compilation)
    {
        foreach (var branch in UnionBranches(type, compilation))
        {
            if (IsType(branch, "Cratis.Chronicle.EventSequences.EventForEventSourceId", compilation))
            {
                continue;
            }

            if (HasEventTypeAttribute(branch, compilation))
            {
                yield return branch;
                continue;
            }

            // EventsCommandResponseValueHandler requires IEnumerable<object>; value-type collections are not covariant.
            // Erased IEnumerable<object> payloads and arbitrary factories cannot be proven from their signatures.
            var element = branch is IArrayTypeSymbol { Rank: 1 } array
                ? array.ElementType
                : branch.AllInterfaces.Prepend(branch).OfType<INamedTypeSymbol>()
                    .FirstOrDefault(candidate => IsType(candidate, "System.Collections.Generic.IEnumerable`1", compilation))?.TypeArguments[0];
            if (element is { IsReferenceType: true } && HasEventTypeAttribute(element, compilation))
            {
                yield return element;
            }
        }
    }

    static bool HasEventTypeAttribute(ITypeSymbol type, Compilation compilation)
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

    static IEnumerable<ITypeSymbol> UnionBranches(ITypeSymbol type, Compilation compilation)
    {
        // Result derives from OneOfBase and the pipeline consumes IOneOf.Value. Restrict to known wrapper definitions;
        // a similarly named generic type (or arbitrary IOneOf implementation) need not expose its type arguments as branches.
        if (type is INamedTypeSymbol named &&
            (IsType(named, $"OneOf.OneOf`{named.Arity}", compilation) ||
             IsType(named, "Cratis.Monads.Result`2", compilation)) &&
            named.AllInterfaces.Any(@interface => IsType(@interface, "OneOf.IOneOf", compilation)))
        {
            return named.TypeArguments.SelectMany(argument => UnionBranches(argument, compilation));
        }

        return [type];
    }

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

    static bool IsType(ITypeSymbol? type, string metadataName, Compilation compilation) =>
        type is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, compilation.GetTypeByMetadataName(metadataName));

    static bool HasAttribute(ISymbol symbol, string metadataName, Compilation compilation) =>
        symbol.GetAttributes().Any(attribute => IsType(attribute.AttributeClass, metadataName, compilation));
}
