// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Prunes command productions atomically when their dependencies are unavailable.
/// </summary>
internal static class CommandProductionPruning
{
    /// <summary>
    /// Removes unavailable mappings without leaving required event payloads incomplete.
    /// </summary>
    /// <param name="production">The original production.</param>
    /// <param name="omitted">The unavailable mappings.</param>
    /// <param name="application">The declared event shapes.</param>
    /// <param name="diagnostics">The loss reports.</param>
    /// <param name="location">The owning command.</param>
    /// <returns>The complete production, or null when a required mapping was removed.</returns>
    internal static ProducesModel? WithoutMappings(ProducesModel production, IReadOnlyList<PropertyMappingModel> omitted, ApplicationModel application, ScreenplayDiagnostics diagnostics, string location)
    {
        if (omitted.Count == 0)
        {
            return production;
        }
        var declaration = application.Slices.SelectMany(slice => slice.Events).FirstOrDefault(@event => production.EventTypeIdentity is { } identity
            ? @event.TypeIdentity == identity : @event.Name == production.EventName);
        var required = omitted.FirstOrDefault(mapping => declaration?.Properties.SingleOrDefault(property => property.Name == mapping.Property)?.Type.IsOptional != true);
        if (required is not null)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnmappableCommandProduction,
                $"Production '{production.EventName}' was left out because required event property '{required.Property}' depends on an unavailable value; removing its mapping would leave the production incomplete",
                location);

            return null;
        }

        return production with { Mappings = production.Mappings.Except(omitted).ToList(), CanInline = false };
    }

    /// <summary>
    /// Withholds routes and successful scenarios that lost their declarative productions.
    /// </summary>
    /// <param name="original">The application before pruning.</param>
    /// <param name="pruned">The application after pruning.</param>
    /// <param name="diagnostics">The loss reports.</param>
    /// <returns>The application without dependent route or scenario claims.</returns>
    internal static ApplicationModel Complete(ApplicationModel original, ApplicationModel pruned, ScreenplayDiagnostics diagnostics) => pruned with
    {
        Slices = original.Slices.Zip(pruned.Slices).Select(pair =>
        {
            var affected = pair.First.Commands.Zip(pair.Second.Commands)
                .Where(commands => commands.First.Produces.Count() > commands.Second.Produces.Count())
                .Select(commands => commands.Second.Name).ToHashSet(StringComparer.Ordinal);
            if (affected.Count == 0)
            {
                return pair.Second;
            }

            var commands = pair.Second.Commands.Select(command =>
            {
                if (!affected.Contains(command.Name))
                {
                    return command;
                }
                if (!command.Produces.Any() && command.Authoring?.Route is not null)
                {
                    diagnostics.Information(
                        ScreenplayDiagnosticCodes.UnreadableCommandRoute,
                        $"Command '{command.Name}': its route was left out because all event productions were withheld; no executable event route can be stated for the remaining handler",
                        pair.Second.Namespace);
                    command = command with { Authoring = command.Authoring with { Route = null } };
                }

                return command with { HasNoFactBehavior = false };
            }).ToList();
            var specifications = pair.Second.Specifications.Where(specification =>
            {
                if (specification.When is not { Kind: SpecificationStateKind.Command } issued || !affected.Contains(issued.Name) || specification.Errors.Any())
                {
                    return true;
                }

                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UnreadableSpecification,
                    $"The scenario '{specification.Name}' was left out because command '{issued.Name}' lost an event production whose outcome cannot be stated faithfully",
                    pair.Second.Namespace);

                return false;
            }).ToList();

            return pair.Second with { Commands = commands, Specifications = specifications };
        }).ToList()
    };
}
