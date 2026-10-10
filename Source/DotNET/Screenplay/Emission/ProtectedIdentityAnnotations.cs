// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Withholds compliance annotations from concepts used as identities, which Screenplay cannot encrypt or erase.
/// </summary>
public static class ProtectedIdentityAnnotations
{
    /// <summary>
    /// Keeps identifiers and routes without making an unsupported compliance claim about their types.
    /// </summary>
    /// <param name="model">The model about to be emitted.</param>
    /// <param name="diagnostics">Where omitted annotations are reported.</param>
    /// <returns>The model with identity annotations withheld.</returns>
    public static ApplicationModel Apply(ApplicationModel model, ScreenplayDiagnostics diagnostics)
    {
        var commands = model.Slices.SelectMany(slice => slice.Commands).ToList();
        var identities = commands.SelectMany(command => command.Properties.Concat(command.Authoring?.Generated ?? [])
                .Where(property => property.Name == (command.Authoring?.Identifier ?? command.Identifier))
                .Select(property => property.Type.Name))
            .Concat(commands.Select(command => command.Authoring?.Route).OfType<CommandRouteModel>()
                .SelectMany(route => new[] { route.IdentifierType?.Name, route.StreamIdType?.Name }.OfType<string>().Concat(route.StreamIdParts.Select(part => part.Type.Name))))
            .ToHashSet(StringComparer.Ordinal);

        return model with
        {
            Concepts = model.Concepts.Select(concept =>
            {
                if (!identities.Contains(concept.Name) || (!concept.IsPii && !concept.IsSensitive))
                {
                    return concept;
                }

                diagnostics.Information(
                    ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation,
                    $"Concept '{concept.Name}' is used as an identifier, destination or stream id; its @pii/@sensitive annotations were left off because Screenplay rejects protected identities (PLAY0515)",
                    concept.Name);

                return concept with { IsPii = false, IsSensitive = false };
            }).ToList()
        };
    }
}
