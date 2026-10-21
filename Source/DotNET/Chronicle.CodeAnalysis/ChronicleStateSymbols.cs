// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Bounded signature recognition shared by Chronicle state-dependency diagnostics.
/// </summary>
internal static class ChronicleStateSymbols
{
    /// <summary>
    /// Determines whether the command supplies its own identity.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether a custom provider is declared.</returns>
    internal static bool HasProvider(INamedTypeSymbol command, Compilation compilation) =>
        Implements(command, "Cratis.Chronicle.Events.ICanProvideEventSourceId", compilation);

    /// <summary>
    /// Matches a framework type or implemented interface by symbol identity.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <param name="metadataName">The framework metadata name.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether the type matches.</returns>
    internal static bool Implements(ITypeSymbol type, string metadataName, Compilation compilation) =>
        RawGuidResponseAnalysis.IsType(type, metadataName, compilation) ||
        type.AllInterfaces.Any(candidate => RawGuidResponseAnalysis.IsType(candidate, metadataName, compilation));

    /// <summary>
    /// Recognizes runtime event source identity ancestry.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether the type is an event source identity.</returns>
    internal static bool IsEventSourceId(ITypeSymbol type, Compilation compilation)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (RawGuidResponseAnalysis.IsType(current, "Cratis.Chronicle.Events.EventSourceId", compilation) ||
                RawGuidResponseAnalysis.IsType(current, "Cratis.Chronicle.Events.EventSourceId`1", compilation))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Recognizes the runtime key-property convention.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether the property declares an event source key.</returns>
    internal static bool IsKey(IPropertySymbol property, Compilation compilation) =>
        IsEventSourceId(property.Type, compilation) || HasKeyAttribute(property, compilation) ||
        property.ContainingType.InstanceConstructors.SelectMany(constructor => constructor.Parameters).Any(parameter =>
            parameter.Name == property.Name && SymbolEqualityComparer.Default.Equals(parameter.Type, property.Type) &&
            RawGuidResponseAnalysis.HasAttribute(parameter, "Cratis.Chronicle.Keys.KeyAttribute", compilation));

    /// <summary>
    /// Recognizes an event-producing model-bound command.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether an event-producing handler is declared.</returns>
    internal static bool IsCommand(INamedTypeSymbol type, Compilation compilation) =>
        RawGuidResponseAnalysis.HasAttribute(type, "Cratis.Arc.Commands.ModelBound.CommandAttribute", compilation) &&
        RawGuidResponseAnalysis.HandleMethods(type).Any(method => ProducesEvents(method.ReturnType, compilation));

    /// <summary>
    /// Finds public instance handlers and public or non-public instance providers.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The handler and provider methods.</returns>
    internal static IEnumerable<IMethodSymbol> DependencyMethods(INamedTypeSymbol command) =>
        RawGuidResponseAnalysis.HandleMethods(command).Concat(ProvideMethods(command));

    /// <summary>
    /// Excludes handler dependencies that could be supplied explicitly.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="command">The command.</param>
    /// <returns>Whether provided values could replace dependency injection.</returns>
    internal static bool MayBeProvided(IMethodSymbol method, INamedTypeSymbol command) =>
        method.Name == "Handle" && ProvideMethods(command).Any();

    /// <summary>
    /// Recognizes explicitly keyed service parameters.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether the parameter selects a keyed service.</returns>
    internal static bool IsKeyedService(IParameterSymbol parameter, Compilation compilation) =>
        RawGuidResponseAnalysis.HasAttribute(parameter, "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute", compilation);

    /// <summary>
    /// Recognizes domain concepts that distinguish an entity identity.
    /// </summary>
    /// <param name="type">The key type.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>Whether the type is distinguishing.</returns>
    internal static bool IsDistinguishing(ITypeSymbol type, Compilation compilation) =>
        type.SpecialType == SpecialType.None &&
        !RawGuidResponseAnalysis.IsType(type, "System.Guid", compilation) &&
        !RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.Events.EventSourceId", compilation) &&
        !RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.Events.EventSourceId`1", compilation) &&
        HasConceptBase(type, compilation);

    /// <summary>
    /// Finds a model's selected key or conventional identity property.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The key property when it can be determined.</returns>
    internal static IPropertySymbol? ModelKey(INamedTypeSymbol model, Compilation compilation)
    {
        var properties = RawGuidResponseAnalysis.PublicProperties(model).Where(property => !property.IsStatic && !property.IsIndexer).ToArray();
        var marked = properties.Where(property => IsKey(property, compilation) && HasModelKeyAttribute(property, compilation)).ToArray();
        if (marked.Length > 0)
        {
            return marked.Length == 1 ? marked[0] : null;
        }
        return properties.FirstOrDefault(property => property.Name == "Id" || property.Name == "id");
    }

    /// <summary>
    /// Collects source-declared projection and reducer targets once per compilation.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The backed model types.</returns>
    internal static ImmutableHashSet<INamedTypeSymbol> BackedTypes(Compilation compilation) =>
        compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>()
            .SelectMany(type => type.AllInterfaces)
            .Where(type => type.TypeArguments.Length == 1 &&
                (RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.Projections.IProjectionFor`1", compilation) ||
                 RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.Reducers.IReducerFor`1", compilation)))
            .Select(type => type.TypeArguments[0]).OfType<INamedTypeSymbol>()
            .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

    /// <summary>
    /// Recognizes statically visible Chronicle backing for a model.
    /// </summary>
    /// <param name="type">The model type.</param>
    /// <param name="compilation">The compilation.</param>
    /// <param name="backedTypes">Lazily collected projection and reducer targets.</param>
    /// <returns>Whether the model has recognizable backing.</returns>
    internal static bool IsBacked(ITypeSymbol type, Compilation compilation, Lazy<ImmutableHashSet<INamedTypeSymbol>> backedTypes) =>
        type is INamedTypeSymbol named && type.SpecialType == SpecialType.None &&
        (HasProjectionMetadata(named) || backedTypes.Value.Contains(named) || IsSelfReducing(named, compilation));

    /// <summary>
    /// Creates a diagnostic with the rule variant's effective severity.
    /// </summary>
    /// <param name="descriptor">The descriptor.</param>
    /// <param name="symbol">The reported symbol.</param>
    /// <param name="severity">The variant severity.</param>
    /// <param name="arguments">The message arguments.</param>
    /// <returns>The diagnostic.</returns>
    internal static Diagnostic Diagnostic(DiagnosticDescriptor descriptor, ISymbol symbol, DiagnosticSeverity severity, params object[] arguments) =>
        Microsoft.CodeAnalysis.Diagnostic.Create(descriptor, symbol.Locations[0], severity, null, null, arguments);

    static bool HasKeyAttribute(IPropertySymbol property, Compilation compilation)
    {
        for (var current = property; current is not null; current = current.OverriddenProperty)
        {
            if (RawGuidResponseAnalysis.HasAttribute(current, "Cratis.Chronicle.Keys.KeyAttribute", compilation))
            {
                return true;
            }
        }
        return false;
    }

    static bool HasModelKeyAttribute(IPropertySymbol property, Compilation compilation) =>
        HasKeyAttribute(property, compilation) || property.ContainingType.InstanceConstructors.SelectMany(constructor => constructor.Parameters)
            .Any(parameter => parameter.Name == property.Name && RawGuidResponseAnalysis.HasAttribute(parameter, "Cratis.Chronicle.Keys.KeyAttribute", compilation));

    static bool HasConceptBase(ITypeSymbol type, Compilation compilation)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (RawGuidResponseAnalysis.IsType(current, "Cratis.Concepts.ConceptAs`1", compilation) || IsEventSourceId(current, compilation))
            {
                return true;
            }
        }
        return false;
    }

    static IEnumerable<IMethodSymbol> ProvideMethods(INamedTypeSymbol command)
    {
        for (var current = command; current is not null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers("Provide").OfType<IMethodSymbol>().Where(method =>
                !method.IsStatic && method.MethodKind == MethodKind.Ordinary &&
                (SymbolEqualityComparer.Default.Equals(current, command) || method.DeclaredAccessibility != Accessibility.Private)))
            {
                yield return method;
            }
        }
    }

    static bool ProducesEvents(ITypeSymbol type, Compilation compilation, int depth = 0)
    {
        // Recursive collection signatures need a bound even when they contain no event metadata.
        if (depth > 8)
        {
            return false;
        }
        type = RawGuidResponseAnalysis.UnwrapAwaitable(type, compilation);
        if (type is INamedTypeSymbol { IsTupleType: true } tuple)
        {
            return tuple.TupleElements.Any(element => ProducesEvents(element.Type, compilation, depth + 1));
        }
        var branches = RawGuidResponseAnalysis.UnionBranches(type, compilation).ToArray();
        if (branches.Length > 1)
        {
            return branches.Any(branch => ProducesEvents(branch, compilation, depth + 1));
        }
        if (RawGuidResponseAnalysis.HasEventTypeAttribute(type, compilation) ||
            RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.EventSequences.EventForEventSourceId", compilation) ||
            RawGuidResponseAnalysis.IsType(type, "Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes", compilation))
        {
            return true;
        }
        var element = type is IArrayTypeSymbol { Rank: 1 } array ? array.ElementType :
            type.AllInterfaces.Prepend(type).OfType<INamedTypeSymbol>().FirstOrDefault(candidate =>
                RawGuidResponseAnalysis.IsType(candidate, "System.Collections.Generic.IEnumerable`1", compilation))?.TypeArguments[0];
        return element is not null && (element.SpecialType == SpecialType.System_Object || ProducesEvents(element, compilation, depth + 1));
    }

    static bool HasProjectionMetadata(INamedTypeSymbol type) =>
        HasProjectionAttribute(type.GetAttributes()) ||
        RawGuidResponseAnalysis.PublicProperties(type).Any(property => HasProjectionAttribute(property.GetAttributes())) ||
        type.InstanceConstructors.Any(constructor => constructor.Parameters.Any(parameter => HasProjectionAttribute(parameter.GetAttributes())));

    static bool HasProjectionAttribute(ImmutableArray<AttributeData> attributes) =>
        attributes.Any(attribute => attribute.AttributeClass is { } type &&
            type.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Projections.ModelBound" &&
            type.Name != "NotProjectedAttribute" && type.Name != "NotRewindableAttribute" && type.Name != "NestedAttribute");

    static bool IsSelfReducing(INamedTypeSymbol type, Compilation compilation) =>
        type is { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false } &&
        type.GetMembers("Reduce").OfType<IMethodSymbol>().Any(method =>
            method is { IsStatic: true, DeclaredAccessibility: Accessibility.Public, IsGenericMethod: false } &&
            method.Parameters.Length is 2 or 3 &&
            method.Parameters.All(parameter => parameter.RefKind == RefKind.None && !parameter.IsOptional && !parameter.IsParams) &&
            RawGuidResponseAnalysis.HasEventTypeAttribute(method.Parameters[0].Type, compilation) &&
            SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, type) &&
            method.Parameters[1].NullableAnnotation != NullableAnnotation.NotAnnotated &&
            (method.Parameters.Length == 2 || RawGuidResponseAnalysis.IsType(method.Parameters[2].Type, "Cratis.Chronicle.Events.EventContext", compilation)) &&
            SymbolEqualityComparer.Default.Equals(RawGuidResponseAnalysis.UnwrapAwaitable(method.ReturnType, compilation), type));
}
