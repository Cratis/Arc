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
    /// the developer wrote.
    /// <para>
    /// Whether a value is personally identifiable is asked of the member rather than of the property alone, because
    /// the canonical way of writing an event is a positional record, and an attribute written there belongs to the
    /// parameter. Chronicle reads it from the constructor parameter too, so reading only the property would leave
    /// the document saying a value is not sensitive when the runtime encrypts it.
    /// </para>
    /// </remarks>
    public IEnumerable<PropertyModel> Read(ITypeSymbol type)
    {
        var properties = new List<PropertyModel>();

        foreach (var property in type.DeclaredProperties())
        {
            if (MemberAttributes.Has(property, WellKnownTypeNames.PiiAttribute))
            {
                types.MarkAsPii(property.Type);
            }

            properties.Add(new(property.Name, types.Resolve(property.Type)));
        }

        return properties;
    }
}
