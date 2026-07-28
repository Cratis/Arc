// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Library;

/// <summary>
/// Shorthands for declaring the parts of a model, so that the fixtures read like the application they describe.
/// </summary>
public static class Declare
{
    /// <summary>
    /// Declares a reference to a single value of a type.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <returns>The type reference.</returns>
    public static TypeReferenceModel Type(string name) => new(name, false, false);

    /// <summary>
    /// Declares a reference to a collection of a type.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <returns>The type reference.</returns>
    public static TypeReferenceModel Many(string name) => new(name, true, false);

    /// <summary>
    /// Declares a reference to an optional value of a type.
    /// </summary>
    /// <param name="name">The name of the type.</param>
    /// <returns>The type reference.</returns>
    public static TypeReferenceModel Maybe(string name) => new(name, false, true);

    /// <summary>
    /// Declares a property.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="type">The name of the type of the property.</param>
    /// <returns>The property.</returns>
    public static PropertyModel Property(string name, string type) => new(name, Type(type));

    /// <summary>
    /// Declares a mapping taking its value from a property of the command.
    /// </summary>
    /// <param name="property">The property being mapped onto.</param>
    /// <param name="path">The path the value is taken from.</param>
    /// <returns>The mapping.</returns>
    public static PropertyMappingModel From(string property, string path) => new(property, new PropertyPathSource(path));

    /// <summary>
    /// Declares a property map for a projection block.
    /// </summary>
    /// <param name="pairs">The read model property to event expression pairs.</param>
    /// <returns>The property map.</returns>
    public static IReadOnlyDictionary<string, string> Map(params (string Property, string Expression)[] pairs) =>
        pairs.ToDictionary(_ => _.Property, _ => _.Expression, StringComparer.Ordinal);
}
