// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Discovers the typed declarations of the application: audiences, availability blocks, provided context values and scope markers.
/// </summary>
public static class TypedAvailability
{
    /// <summary>
    /// Finds the audiences declared as types.
    /// </summary>
    /// <param name="types">The types of the application, or <see langword="null"/> when there are none to discover.</param>
    /// <returns>The declarations, ordered by name.</returns>
    public static IReadOnlyList<AudienceDeclaration> Audiences(ITypes? types) =>
        [.. Concrete(types, typeof(Audience))
            .Select(_ => new AudienceDeclaration(AudienceName.Of(_), string.Empty, _.FullName ?? _.Name))
            .OrderBy(_ => _.Name.Value, StringComparer.Ordinal)];

    /// <summary>
    /// Creates the catalog of the availability blocks declared as types.
    /// </summary>
    /// <param name="services">The services, which create the conditions that inject.</param>
    /// <returns>The catalog.</returns>
    public static AvailabilityCatalog CreateCatalog(IServiceProvider services)
    {
        var catalog = new AvailabilityCatalog();
        var conditions = ConditionFactory(services);
        foreach (var block in Concrete(services.GetService<ITypes>(), typeof(AvailabilityBlock)).OrderBy(_ => _.FullName, StringComparer.Ordinal))
        {
            catalog.Add(((AvailabilityBlock)ActivatorUtilities.CreateInstance(services, block)).Define(conditions));
        }

        return catalog;
    }

    /// <summary>
    /// Creates the catalog of the context values provided with <see cref="ProvideContextAttribute{TReadModel}"/>.
    /// </summary>
    /// <param name="services">The services, which hold the query the host registers.</param>
    /// <returns>The catalog.</returns>
    /// <exception cref="InvalidOperationException">A value is provided and no <see cref="IReadModelQuery"/> is registered.</exception>
    public static ContextValueCatalog CreateContextValues(IServiceProvider services)
    {
        var catalog = new ContextValueCatalog();
        var types = services.GetService<ITypes>();
        foreach (var type in (types?.All ?? []).OrderBy(_ => _.FullName, StringComparer.Ordinal))
        {
            foreach (var attribute in type.GetCustomAttributes(false).Where(_ => _.GetType().IsGenericType && _.GetType().GetGenericTypeDefinition() == typeof(ProvideContextAttribute<>)))
            {
                var query = services.GetService<IReadModelQuery>() ??
                    throw new InvalidOperationException($"{type.FullName} provides a context value, which needs an IReadModelQuery registered by the host.");
                var provide = attribute.GetType();
                var readModel = (Type)provide.GetProperty(nameof(ProvideContextAttribute<object>.ReadModel))!.GetValue(attribute)!;
                var queryName = (string)provide.GetProperty(nameof(ProvideContextAttribute<object>.Query))!.GetValue(attribute)!;
                var key = (string)provide.GetProperty(nameof(ProvideContextAttribute<object>.Key))!.GetValue(attribute)!;
                var required = (bool)provide.GetProperty(nameof(ProvideContextAttribute<object>.Required))!.GetValue(attribute)!;
                catalog.Declare(new(
                    ContextNames.Of(readModel),
                    new ReadModelContextValueProvider(query, queryName, key),
                    ScopeOf(type),
                    required));
            }
        }

        return catalog;
    }

    /// <summary>
    /// Gets the context scope of a type, which is its namespace.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The scope.</returns>
    public static ContextScope ScopeOf(Type type) =>
        string.IsNullOrEmpty(type.Namespace) ? ContextScope.Application : ContextScope.Of(type.Namespace.Split('.'));

    /// <summary>
    /// Finds the gates of the scope markers: everything in the namespace of a marker is gated by what the marker states.
    /// </summary>
    /// <param name="types">The types of the application.</param>
    /// <param name="conditions">Creates the condition of an <see cref="IAvailabilityCondition"/> class.</param>
    /// <returns>The namespaces and the gates that hold for them.</returns>
    public static IReadOnlyList<(string Namespace, AvailabilityExpression Gate)> Markers(ITypes? types, Func<Type, IAvailabilityCondition> conditions) =>
        [.. Concrete(types, typeof(AvailabilityScope))
            .Where(_ => !string.IsNullOrEmpty(_.Namespace))
            .SelectMany(marker => Gates(marker, conditions).Select(gate => (marker.Namespace!, gate)))];

    /// <summary>
    /// Finds the namespaces of the scope markers.
    /// </summary>
    /// <param name="types">The types of the application.</param>
    /// <returns>The namespaces, which are gated.</returns>
    public static IReadOnlyList<string> MarkerNamespaces(ITypes? types) =>
        [.. Concrete(types, typeof(AvailabilityScope)).Select(_ => _.Namespace).OfType<string>().Where(_ => _.Length > 0).Distinct()];

    /// <summary>
    /// Reads the typed gates a part states with attributes.
    /// </summary>
    /// <param name="part">The type or member that carries the attributes.</param>
    /// <param name="conditions">Creates the condition of an <see cref="IAvailabilityCondition"/> class.</param>
    /// <returns>One expression for every attribute, all of which must hold.</returns>
    public static IEnumerable<AvailabilityExpression> Gates(MemberInfo part, Func<Type, IAvailabilityCondition> conditions) =>
        part.GetCustomAttributes(false).OfType<IAvailabilityAttribute>().Select(_ => _.Term.ToExpression(conditions));

    /// <summary>
    /// Creates the factory of conditions that are classes, which injects what they ask for.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The factory.</returns>
    public static Func<Type, IAvailabilityCondition> ConditionFactory(IServiceProvider? services) =>
        type => (IAvailabilityCondition)(services is null ? Activator.CreateInstance(type, nonPublic: true)! : ActivatorUtilities.CreateInstance(services, type));

    static IEnumerable<Type> Concrete(ITypes? types, Type baseType) =>
        (types?.All ?? []).Where(_ => _.IsClass && !_.IsAbstract && baseType.IsAssignableFrom(_));
}
