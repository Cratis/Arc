// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reads the availability gates a type carries as attributes: <c>[AvailableFor&lt;Audience&gt;]</c>, <c>[Available&lt;Block&gt;]</c>
/// and <c>[AvailableWhen&lt;Condition&gt;]</c>, and the obsolete string forms <c>[AvailableFor("name")]</c> and <c>[AvailableWith("name")]</c>.
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

        var gates = Gates(type).ToList();

        return gates.Count == 0 ? null : new(gates);
    }

    /// <summary>
    /// Reads the gates a type carries, one for every attribute.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The gates, in the order of the attributes.</returns>
    public static IEnumerable<AvailabilityGateModel> Gates(ISymbol type) =>
        type.GetAttributes().Select(Gate).OfType<AvailabilityGateModel>();

    static AvailabilityGateModel? Gate(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true, TypeArguments.Length: > 1 } &&
            string.Equals(attributeClass.OriginalDefinition.ContainingNamespace.ToDisplayString(), "Cratis.Arc.Availability", StringComparison.Ordinal) &&
            attributeClass.OriginalDefinition.Name == "AvailableForAttribute")
        {
            return AvailabilityGateModel.ForAudiences(attributeClass.TypeArguments.Select(_ => _.Name));
        }

        if (attributeClass is { IsGenericType: true, TypeArguments.Length: 1 })
        {
            var argument = attributeClass.TypeArguments[0].Name;
            if (attributeClass.Is(WellKnownTypeNames.AvailableForAudienceAttribute))
            {
                return new(new AudienceRequirementModel(argument));
            }

            if (attributeClass.Is(WellKnownTypeNames.AvailableBlockAttribute))
            {
                return new(new ReferenceRequirementModel(argument));
            }

            if (attributeClass.Is(WellKnownTypeNames.AvailableWhenAttribute))
            {
                return new(new ConditionRequirementModel(new OpaqueConditionModel(AvailabilityTerms.OpaqueName(argument))));
            }

            return null;
        }

        if (attributeClass.Is(WellKnownTypeNames.AvailableForAttribute))
        {
            var audiences = attribute.ConstructorArguments
                .SelectMany(_ => _.Kind == TypedConstantKind.Array ? _.Values.Select(value => value.Value) : [_.Value])
                .OfType<string>()
                .ToList();

            return audiences.Count == 0 ? null : AvailabilityGateModel.ForAudiences(audiences);
        }

        if (attributeClass.Is(WellKnownTypeNames.AvailableWithAttribute) &&
            attribute.ConstructorArguments.FirstOrDefault().Value is string name)
        {
            return AvailabilityGateModel.WithAvailability(name);
        }

        return null;
    }
}
