// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Types;

/// <summary>
/// Finds carried shapes emptied by property omission, which cannot be declared in Screenplay.
/// </summary>
internal static class EmptyShapes
{
    /// <summary>
    /// Finds shapes losing every property, including references to other emptied shapes.
    /// </summary>
    /// <param name="model">The original application.</param>
    /// <returns>The names whose references must also be omitted.</returns>
    internal static IReadOnlySet<string> In(ApplicationModel model)
    {
        var empty = new HashSet<string>(StringComparer.Ordinal);
        var shapes = model.Types.Select(type => (type.Name, type.Properties))
            .Concat(model.Slices.SelectMany(slice => slice.ReadModels).Select(readModel => (readModel.Name, readModel.Properties)))
            .Where(shape => shape.Properties.Any()).ToList();
        while (shapes.Exists(type => type.Properties.All(property => property.Type.UnnameableType is not null || empty.Contains(property.Type.Name)) && empty.Add(type.Name)))
        {
        }

        return empty;
    }
}
