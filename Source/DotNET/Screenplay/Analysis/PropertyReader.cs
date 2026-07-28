// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the properties a command, an event or a model declares.
/// </summary>
/// <param name="types">The <see cref="TypeRegistry"/> resolving the type of each property.</param>
public class PropertyReader(TypeRegistry types)
{
    /// <summary>
    /// Reads the properties of a type, in the order the source declares them.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The properties.</returns>
    /// <remarks>
    /// Declaration order is kept rather than sorted, because the order of a record's positional parameters is what
    /// the developer wrote and is stable for a given compilation.
    /// </remarks>
    public IEnumerable<PropertyModel> Read(ITypeSymbol type)
    {
        var properties = new List<PropertyModel>();

        foreach (var property in type.DeclaredProperties())
        {
            if (property.HasAttribute(WellKnownTypeNames.PiiAttribute))
            {
                types.MarkAsPii(property.Type);
            }

            properties.Add(new(property.Name, types.Resolve(property.Type)));
        }

        return properties;
    }
}
