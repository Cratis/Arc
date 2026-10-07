// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Admits recovered command values before inline events and destinations are selected.
/// </summary>
/// <param name="diagnostics">Where values outside ESM v7 are reported.</param>
public class ExecutableCommandValues(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Removes syntax-only intent and unadmitted generated values from the default document.
    /// </summary>
    /// <param name="model">The full application, including concept rules.</param>
    /// <returns>The model used consistently by command, event, and specification emission.</returns>
    public ApplicationModel Apply(ApplicationModel model) => model with
    {
        Slices = model.Slices.Select(slice => slice with
        {
            Commands = slice.Commands.Select(command => Admit(command, model, slice.Namespace)).OfType<CommandModel>().ToList()
        }).ToList()
    };

    static bool CanGenerate(PropertyModel property, CommandAuthoringModel authoring, ApplicationModel model) =>
        !property.Type.IsOptional && !property.Type.IsCollection &&
        !authoring.GeneratedWithValidators.Contains(property.Name, StringComparer.Ordinal) &&
        model.Concepts.SingleOrDefault(concept => concept.Name == property.Type.Name) is { Primitive: ScreenplayPrimitive.Uuid } concept && !concept.Validations.Any();

    static bool Reads(string path, string name) => string.Equals(path, name, StringComparison.OrdinalIgnoreCase) || path.StartsWith($"{name}.", StringComparison.OrdinalIgnoreCase);

    static bool Reads(PropertyPathSource? source, string name) => source is not null && Reads(source.Path, name);

    static bool Reads(ConditionModel condition, string name) => condition switch
    {
        ComparisonCondition comparison => Reads(comparison.Left, name) || Reads(comparison.Right as PropertyPathSource, name),
        LogicalCondition logical => Reads(logical.Left, name) || Reads(logical.Right, name),
        _ => false
    };

    CommandModel? Admit(CommandModel command, ApplicationModel model, string location)
    {
        if (command.Authoring is not { } authoring)
        {
            return WithoutCodeOnlyBehavior(command, location);
        }

        var blocked = authoring.Generated.Where(property => !CanGenerate(property, authoring, model)).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var protectedValues = authoring.Generated.Where(property => command.Validations.Any(rule => Reads(rule.Property, property.Name) || Reads(rule.Value as PropertyPathSource, property.Name)) ||
            authoring.Requirements.Any(requirement => Reads(requirement.Condition, property.Name))).ToList();
        if (protectedValues.Count > 0)
        {
            Report(command, location, "Generated values are referenced by pre-generation property rules or requirements (PLAY0273); the command was left in code rather than dropping its protection");
            return null;
        }

        if (blocked.Count > 0)
        {
            Report(command, location, $"Generated values '{string.Join(", ", blocked.Order(StringComparer.Ordinal))}' can be generated only as required scalar Uuid-backed concepts with no concept validators or validation rules (PLAY0268); those values, dependent responses, mappings, and production conditions were left in code");
        }

        var retained = authoring with
        {
            Generated = authoring.Generated.Where(property => !blocked.Contains(property.Name)).ToList(),
            Identifier = authoring.Identifier is { } identifier && blocked.Contains(identifier) ? null : authoring.Identifier,
            Response = authoring.Response is { } response && blocked.Contains(response) ? null : authoring.Response,
            ResponseFields = authoring.ResponseFields.Any(field => field.Source is PropertyPathSource source && blocked.Contains(source.Path)) ? [] : authoring.ResponseFields,
            Operations = [],
            Route = null,
            Reads = [],
            Requirements = []
        };
        if (authoring.Identifier is not null && retained.Identifier is null)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult,
                "The handler yields an event source identifier or response alongside the event, but its generated identity is not admitted by ESM v7; no event source destination was inferred",
                location);
        }

        var productions = new List<ProducesModel>();
        foreach (var production in command.Produces)
        {
            if (production.When is not null && blocked.Any(name => Reads(production.When, name)))
            {
                Report(command, location, $"Production '{production.EventName}' was omitted because its when condition reads an unadmitted generated value");
                continue;
            }

            var omitted = production.Mappings.Where(mapping => mapping.Source is PropertyPathSource source && blocked.Any(name => Reads(source, name))).ToList();
            var declaration = model.Slices.SelectMany(slice => slice.Events).FirstOrDefault(@event => production.EventTypeIdentity is { } identity
                ? @event.TypeIdentity == identity : @event.Name == production.EventName);
            foreach (var mapping in omitted)
            {
                if (declaration?.Properties.SingleOrDefault(property => property.Name == mapping.Property)?.Type.IsOptional != true)
                {
                    var value = blocked.First(name => Reads((PropertyPathSource)mapping.Source, name));
                    Report(command, location, $"The command was left out because its production '{production.EventName}' needs the generated value '{value}', which cannot be stated");
                    return null;
                }
            }

            var mappings = production.Mappings.Except(omitted).ToList();
            if (mappings.Count != production.Mappings.Count())
            {
                Report(command, location, $"Mappings on production '{production.EventName}' reading unadmitted generated values were omitted; the production was retained as a standalone event");
            }

            productions.Add(production with
            {
                Mappings = mappings,
                CanInline = production.CanInline && mappings.Count == production.Mappings.Count(),
                UsesCommandContext = production.UsesCommandContext && !(authoring.Identifier is not null && retained.Identifier is null)
            });
        }

        return WithoutCodeOnlyBehavior(command with { Authoring = retained, Produces = productions }, location);
    }

    CommandModel? WithoutCodeOnlyBehavior(CommandModel command, string location)
    {
        if (command.Produces.Any() || command.HasNoFactBehavior)
        {
            return command;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.CommandBehaviorInCode,
            "The command was left out because its behavior lives in code and no production was recovered; ScreenplayOptions.AuthoringOnlyConstructs keeps it with a handler reference",
            $"{location}.{command.Name}");
        return null;
    }

    void Report(CommandModel command, string location, string reason) => diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandResponse, reason, $"{location}.{command.Name}");
}
