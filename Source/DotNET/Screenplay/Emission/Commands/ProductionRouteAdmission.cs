// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Admits complete per-production contracts before defaults or declarations can imply a different route.
/// </summary>
internal static class ProductionRouteAdmission
{
    /// <summary>
    /// Gets every route actually referenced by a command and its event productions.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The complete routes, including per-production overrides.</returns>
    internal static IEnumerable<CommandRouteModel> Routes(CommandModel command) =>
        command.Produces.Select(production => production.Route).Prepend(command.Authoring?.Route).OfType<CommandRouteModel>();

    /// <summary>
    /// Withholds inadmissible or capped overrides and dependent successful scenarios in both output modes.
    /// </summary>
    /// <param name="model">The analyzed application.</param>
    /// <param name="diagnostics">The located omission reports.</param>
    /// <param name="maximumVersion">The executable version cap.</param>
    /// <returns>The application with admitted production routes.</returns>
    internal static ApplicationModel Apply(ApplicationModel model, ScreenplayDiagnostics diagnostics, SemanticVersion? maximumVersion)
    {
        return model with
        {
            Slices = model.Slices.Select(slice =>
            {
                var withheld = new HashSet<string>(StringComparer.Ordinal);
                var commands = slice.Commands.Select(command => command with
                {
                    Produces = command.Produces.Select(production =>
                    {
                        if (production.Route is not { } route)
                        {
                            return production;
                        }
                        var reason = maximumVersion is { } cap && !cap.IsAtLeast(SemanticVersion.V10)
                            ? $"production route overrides require ESM v10, but ScreenplayOptions.MaximumExecutableModelVersion is capped at ESM v{cap}"
                            : CommandRouteAdmission.Failure(route, command, model, authoring: false);
                        if (reason is null)
                        {
                            return production;
                        }
                        diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandRoute, $"Command '{command.Name}', production '{production.EventName}': {reason}; its destination and entire override were withheld, never replaced with command defaults", $"{slice.Namespace}.{command.Name}");
                        withheld.Add(command.Name);

                        return production with { Route = null, Destination = null, UsesCommandContext = false, CanInline = false };
                    }).ToList()
                }).Select(WithoutUnknownSiblings).ToList();
                var specifications = slice.Specifications.Where(specification =>
                {
                    if (specification.When is not { Kind: SpecificationStateKind.Command } issued || !withheld.Contains(issued.Name) || specification.Errors.Any())
                    {
                        return true;
                    }
                    diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableSpecification, $"The scenario '{specification.Name}' was left out because command '{issued.Name}' has a withheld production route", slice.Namespace);

                    return false;
                }).ToList();

                return slice with { Commands = commands, Specifications = specifications };
            }).ToList()
        };
    }

    static CommandModel WithoutUnknownSiblings(CommandModel command)
    {
        var productions = command.Produces.ToList();
        var unknown = productions.Exists(production => !production.UsesCommandContext && production.Destination is null);
        var missingContext = productions.Exists(production => production.Destination is not null) &&
            productions.Exists(production => production.UsesCommandContext && production.Destination is null) &&
            (command.Authoring?.Identifier ?? command.Identifier) is null;
        if (!unknown && !missingContext)
        {
            return command;
        }

        return command with
        {
            Produces = productions.ConvertAll(production => production with
            {
                Destination = null,
                Route = null,
                UsesCommandContext = production.UsesCommandContext && !missingContext
            })
        };
    }
}
