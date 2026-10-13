// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Keys;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Reads the identifier that Chronicle's materialization maps from the document key.
/// </summary>
internal static class ObservationReadModelKey
{
    /// <summary>
    /// Finds the identifier using Chronicle's materialization precedence: key, subject, then Id.
    /// </summary>
    /// <param name="type">The read model type.</param>
    /// <returns>The readable identifier property, or null.</returns>
    internal static PropertyInfo? PropertyFor(Type type)
    {
        var properties = type.GetProperties();
        var parameters = type.GetConstructors().FirstOrDefault()?.GetParameters() ?? [];
        var property = Find<KeyAttribute>(properties, parameters)
            ?? Find<SubjectAttribute>(properties, parameters)
            ?? properties.FirstOrDefault(property => property.Name == "Id");

        return IsReadable(property) ? property : null;
    }

    /// <summary>
    /// Gets the identifier of a materialized instance.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="instance">The materialized instance.</param>
    /// <param name="property">The identifier property.</param>
    /// <returns>The unwrapped identifier.</returns>
    /// <exception cref="PinnedReadModelCannotBeObserved">The identifier has no value.</exception>
    internal static string Of<TReadModel>(TReadModel instance, PropertyInfo property)
    {
        var value = property.GetValue(instance);
        var key = (value?.IsConcept() == true ? value.GetConceptValue() : value)?.ToString();

        return key ?? throw new PinnedReadModelCannotBeObserved(typeof(TReadModel));
    }

    static bool IsReadable(PropertyInfo? property)
    {
        if (property?.GetMethod?.IsPublic != true)
        {
            return false;
        }

        return property.GetIndexParameters().Length == 0;
    }

    static PropertyInfo? Find<TAttribute>(PropertyInfo[] properties, ParameterInfo[] parameters)
        where TAttribute : Attribute
    {
        var property = properties.FirstOrDefault(property => Attribute.IsDefined(property, typeof(TAttribute)));
        var parameter = parameters.FirstOrDefault(parameter => Attribute.IsDefined(parameter, typeof(TAttribute)));

        return property ?? properties.FirstOrDefault(property => property.Name == parameter?.Name);
    }
}
