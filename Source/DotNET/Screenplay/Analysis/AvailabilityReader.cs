// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the availability gates a type carries as <c>[AvailableFor]</c> and <c>[AvailableWith]</c> attributes.
/// </summary>
public static class AvailabilityReader
{
    /// <summary>
    /// Reads the gates a type carries.
    /// </summary>
    /// <param name="type">The command or read model type to read.</param>
    /// <returns>The <see cref="AvailabilityModel"/>, or <see langword="null"/> when the type carries no gate.</returns>
    public static AvailabilityModel? Read(ISymbol? type)
    {
        if (type is null)
        {
            return null;
        }

        var gates = type.GetAttributes().Select(Gate).OfType<AvailabilityGateModel>().ToList();

        return gates.Count == 0 ? null : new(gates);
    }

    static AvailabilityGateModel? Gate(AttributeData attribute)
    {
        if (attribute.AttributeClass.Is(WellKnownTypeNames.AvailableForAttribute))
        {
            var audiences = attribute.ConstructorArguments
                .SelectMany(_ => _.Kind == TypedConstantKind.Array ? _.Values.Select(value => value.Value) : [_.Value])
                .OfType<string>()
                .ToList();

            return new(audiences);
        }

        if (attribute.AttributeClass.Is(WellKnownTypeNames.AvailableWithAttribute) &&
            attribute.ConstructorArguments.FirstOrDefault().Value is string name)
        {
            return new([], name);
        }

        return null;
    }
}
