// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Types;

/// <summary>
/// Tracks schemas losing properties, including records carrying an incomplete nested shape.
/// </summary>
internal sealed class IncompleteShapes
{
    readonly HashSet<string> _names = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="IncompleteShapes"/> class.
    /// </summary>
    /// <param name="model">The original schemas before property omission.</param>
    internal IncompleteShapes(ApplicationModel model)
    {
        var shapes = model.Types.Select(type => (type.Name, type.Properties))
            .Concat(model.Slices.SelectMany(slice => slice.Events).Select(@event => (@event.Name, @event.Properties)))
            .Concat(model.Slices.SelectMany(slice => slice.ReadModels).Select(readModel => (readModel.Name, readModel.Properties)))
            .Concat(model.Slices.SelectMany(slice => slice.Commands).Select(command => (command.Name, command.Properties)))
            .ToList();

        // Each successful pass adds a previously unseen name; recursive records therefore terminate.
        while (shapes.Exists(shape => Affects(shape.Properties) && _names.Add(shape.Name)))
        {
        }
    }

    /// <summary>
    /// Determines whether a declaration has an incomplete schema.
    /// </summary>
    /// <param name="name">The declaration name.</param>
    /// <returns>True when a reachable property is unnameable.</returns>
    internal bool Contains(string name) => _names.Contains(name);

    /// <summary>
    /// Determines whether properties carry an unnameable or incomplete shape.
    /// </summary>
    /// <param name="properties">The properties to inspect.</param>
    /// <returns>True when at least one property is affected.</returns>
    internal bool Affects(IEnumerable<PropertyModel> properties) => properties.Any(property =>
        property.Type.UnnameableType is not null || Contains(property.Type.Name));
}
