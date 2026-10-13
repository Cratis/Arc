// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Applies version-dependent fixture checks after concept generation declarations are admitted.
/// </summary>
internal static class ExecutableGenerationFixtures
{
    /// <summary>
    /// Marks invalid implicit UUID fixtures without changing legacy model acceptance.
    /// </summary>
    /// <param name="model">The application with admitted command values.</param>
    /// <param name="concepts">The admitted concept declarations determining fixture semantics.</param>
    /// <returns>The application with unrepresentable fixtures marked for scenario withholding.</returns>
    public static ApplicationModel Apply(ApplicationModel model, IReadOnlyList<ConceptSyntax> concepts)
    {
        if (!concepts.Any(concept => concept.Generation is not null))
        {
            return model;
        }

        return model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Specifications = slice.Specifications.Select(specification => Check(specification, model)).ToList()
            }).ToList()
        };
    }

    static SpecificationModel Check(SpecificationModel specification, ApplicationModel model)
    {
        if (specification.GenerationFailure is not null)
        {
            return specification;
        }
        if (specification.When is not { Kind: SpecificationStateKind.Command } issued)
        {
            return specification;
        }
        var command = model.Slices.SelectMany(slice => slice.Commands).FirstOrDefault(command => command.Name == issued.Name);
        var invalid = specification.GeneratedValues.Any(pin => IsInvalidUuid(pin, command, model));

        return invalid ? specification with { GenerationFailure = "its UUID generation fixture must be version 4 with the RFC variant in an ESM v15 document" } : specification;
    }

    static bool IsInvalidUuid(PropertyMappingModel pin, CommandModel? command, ApplicationModel model)
    {
        var type = command?.Authoring?.Generated.SingleOrDefault(property => property.Name == pin.Property)?.Type.Name;
        if (type is null)
        {
            return false;
        }
        var concept = model.Concepts.SingleOrDefault(concept => concept.Name == type);
        if (concept is null || !UsesUuid(concept))
        {
            return false;
        }

        return pin.Source is LiteralSource literal && !GenerationFixtureShape.MatchesUuidV4(literal);
    }

    static bool UsesUuid(ConceptModel concept)
    {
        if (concept.Generation is { } generation)
        {
            return generation.Kind == "uuid";
        }

        return concept.Primitive == ScreenplayPrimitive.Uuid;
    }
}
