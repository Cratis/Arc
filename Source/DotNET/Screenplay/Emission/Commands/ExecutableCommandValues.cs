// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Semantics;

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
    public ApplicationModel Apply(ApplicationModel model) => Apply(model, null);

    /// <summary>
    /// Removes generated values and responses outside the requested executable version.
    /// </summary>
    /// <param name="model">The full application, including concept rules.</param>
    /// <param name="maximumVersion">The executable model version cap, or null for the latest constructs.</param>
    /// <param name="authoringOnlyConstructs">Whether to retain additional authoring-only constructs.</param>
    /// <returns>The model used consistently by command, event, and specification emission.</returns>
    public ApplicationModel Apply(ApplicationModel model, SemanticVersion? maximumVersion, bool authoringOnlyConstructs = false) => CommandProductionPruning.Complete(
        model,
        model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => Admit(command, new(model, maximumVersion, authoringOnlyConstructs), slice.Namespace)).ToList()
            }).ToList()
        },
        diagnostics);

    static bool Reads(string path, string name) => string.Equals(path, name, StringComparison.OrdinalIgnoreCase) || path.StartsWith($"{name}.", StringComparison.OrdinalIgnoreCase);

    static bool Reads(PropertyPathSource? source, string name) => source is not null && Reads(source.Path, name);

    static bool Reads(ConditionModel condition, string name) => condition switch
    {
        ComparisonCondition comparison => Reads(comparison.Left, name) || Reads(comparison.Right as PropertyPathSource, name),
        LogicalCondition logical => Reads(logical.Left, name) || Reads(logical.Right, name),
        _ => false
    };

    static bool ReadsProtected(ConditionModel condition, CommandAuthoringModel authoring) => authoring.Reads.Any(read => read.Protected && Reads(condition, read.Alias));

    static bool NeedsLegacyFixtures(CommandAuthoringModel authoring, List<SpecificationModel> scenarios)
    {
        if (authoring.GeneratedParameters)
        {
            return false;
        }

        return scenarios.Count > 0 && authoring.Generated.Count > 0;
    }

    static bool MustPreserveExplicitSources(CommandAuthoringModel authoring, ApplicationModel model, CommandModel command)
    {
        if (authoring.GeneratedParameters || authoring.Generated.Count == 0)
        {
            return false;
        }

        return model.Slices.SelectMany(slice => slice.Specifications).Any(specification => HasExplicitSource(specification, command));
    }

    static bool HasExplicitSource(SpecificationModel specification, CommandModel command)
    {
        if (specification.When is not { Kind: SpecificationStateKind.Command } issued || issued.Name != command.Name)
        {
            return false;
        }

        return !ResponseOnlyScenarios.IsResponseOnly(specification) && SpecificationEvidence.For(specification) is { HasExplicitCommandSources: true };
    }

    static bool RuleReads(ValidationRuleModel rule, string name) => Reads(rule.Property, name) || Reads(rule.Value as PropertyPathSource, name);

    static bool ReadsGenerated(CommandAdmissionContext context, PropertyModel property) =>
        context.Command.Validations.Any(rule => RuleReads(rule, property.Name)) ||
        context.Authoring.Requirements.Any(requirement => Reads(requirement.Condition, property.Name));

    static bool HasRequiredMapping(CommandAdmissionContext context) =>
        context.Command.Produces.Any(production => production.Mappings.Any(mapping => IsRequiredBlockedMapping(context, production, mapping)));

    static bool IsRequiredBlockedMapping(CommandAdmissionContext context, ProducesModel production, PropertyMappingModel mapping)
    {
        if (mapping.Source is not PropertyPathSource source || !context.Blocked.Any(name => Reads(source, name)))
        {
            return false;
        }

        var declared = context.Scope.Model.Slices.SelectMany(slice => slice.Events).FirstOrDefault(@event => production.EventTypeIdentity is { } identity
            ? @event.TypeIdentity == identity
            : @event.Name == production.EventName);
        return declared?.Properties.SingleOrDefault(property => property.Name == mapping.Property)?.Type.IsOptional != true;
    }

    static List<SpecificationModel> SuccessfulScenarios(CommandAdmissionContext context) =>
        context.Scope.Model.Slices.SelectMany(slice => slice.Specifications).Where(specification =>
            specification.When is { Kind: SpecificationStateKind.Command } issued && issued.Name == context.Command.Name &&
            !specification.Errors.Any() && specification.Then.Any()).ToList();

    static bool HasUnrecoveredReturns(CommandAuthoringModel authoring, List<SpecificationModel> scenarios) =>
        (authoring.Response is not null || authoring.ResponseFields.Count > 0) &&
        scenarios.Exists(specification => specification.AssertsResponse && specification.Returns?.Fits(authoring) != true);

    static string? RetainedName(string? name, CommandAdmissionContext context) =>
        context.Legacy || (name is { } declared && context.Blocked.Contains(declared)) ? null : name;

    static IReadOnlyList<PropertyMappingModel> RetainedResponseFields(CommandAdmissionContext context) =>
        context.Legacy || context.Authoring.ResponseFields.Any(field => field.Source is PropertyPathSource source && context.Blocked.Contains(source.Path))
            ? []
            : context.Authoring.ResponseFields;

    static CommandRouteModel? RetainedRoute(CommandAdmissionContext context) =>
        context.Scope.AuthoringOnlyConstructs || context.Scope.MaximumVersion is not { } routeCap || routeCap.IsAtLeast(SemanticVersion.V8)
            ? context.Authoring.Route
            : null;

    static CommandAuthoringModel Retain(CommandAdmissionContext context)
    {
        var authoring = context.Authoring;
        var authoringOnlyConstructs = context.Scope.AuthoringOnlyConstructs;
        return authoring with
        {
            Generated = authoring.Generated.Where(property => !context.Blocked.Contains(property.Name)).ToList(),
            Identifier = RetainedName(authoring.Identifier, context),
            Response = RetainedName(authoring.Response, context),
            ResponseFields = RetainedResponseFields(context),
            Operations = authoringOnlyConstructs ? authoring.Operations : [],
            Route = RetainedRoute(context),
            Reads = authoringOnlyConstructs ? authoring.Reads : authoring.Reads.Where(read => read.Protected).ToList(),
            Requirements = authoringOnlyConstructs ? authoring.Requirements : authoring.Requirements.Where(requirement => ReadsProtected(requirement.Condition, authoring)).ToList()
        };
    }

    static bool DroppedIdentifier(CommandAdmissionContext context, CommandAuthoringModel retained) =>
        context.Authoring.Identifier is not null && retained.Identifier is null;

    static bool ReadsUnadmittedDestination(CommandAdmissionContext context, ProducesModel production)
    {
        var destinationPaths = production.DestinationDependencies
            .Concat(production.Destination is PropertyPathSource destination ? [destination.Path] : [])
            .Concat(production.Route?.StreamId is { } streamId ? [streamId] : []);
        return destinationPaths.Any(path => context.Blocked.Any(name => Reads(path, name)));
    }

    static string? OmissionReason(CommandAdmissionContext context, ProducesModel production)
    {
        if (ReadsUnadmittedDestination(context, production))
        {
            return $"Production '{production.EventName}' was omitted because its explicit destination or route reads an unadmitted generated value; it was not redirected to command context";
        }

        return production.When is not null && context.Blocked.Any(name => Reads(production.When, name))
            ? $"Production '{production.EventName}' was omitted because its when condition reads an unadmitted generated value"
            : null;
    }

    CommandModel WithoutAdvisoryReads(CommandModel command, ApplicationModel model, string location) => AuthoringDeclarations.WithoutReads(
        command,
        model,
        command.Authoring?.Reads.Where(read => !read.Protected).Select(read => read.Alias).ToHashSet(StringComparer.Ordinal) ?? [],
        diagnostics,
        location,
        $"Command '{command.Name}': an omitted read model or withheld authoring read cannot be read; its dependency, requirements and mappings were left in code");

    CommandModel Admit(CommandModel command, CommandAdmissionScope scope, string location)
    {
        var model = scope.Model;
        command = new UnavailableCommandValues(diagnostics).Apply(command, model, $"{location}.{command.Name}");
        command = scope.AuthoringOnlyConstructs ? command : WithoutAdvisoryReads(command, model, location);
        if (command.Authoring is not { } authoring)
        {
            return command;
        }

        var capped = CommandValueAdmission.RequiresLegacyValues(authoring, scope.MaximumVersion);
        if (CommandValueAdmission.KeepsAuthoring(authoring, model, scope.MaximumVersion, scope.AuthoringOnlyConstructs))
        {
            return command;
        }
        if (capped)
        {
            Report(command, location, $"Generation and responses were withheld because ScreenplayOptions.MaximumExecutableModelVersion is capped at ESM v{scope.MaximumVersion}; ESM v7 is required, so the command retains its legacy productions or handler reference");
        }

        var blocked = new CommandValueAdmission(diagnostics).Blocked(command, model, location, scope.MaximumVersion);
        var context = new CommandAdmissionContext(command, scope, location, authoring, blocked, capped);
        var reasons = ReportLegacyReasons(context);
        context = context with { Legacy = capped || reasons };
        if (context.Legacy)
        {
            blocked.UnionWith(authoring.Generated.Select(property => property.Name));
        }

        var retained = Retain(context);
        ReportDroppedIdentifier(context, retained);

        var productions = AdmitProductions(context, retained);
        return command with
        {
            Authoring = WithAdmittedRoute(context, retained, productions),
            Produces = productions,
            Validations = command.Validations.Where(rule => !blocked.Any(name => RuleReads(rule, name))).ToList(),
            HasNoFactBehavior = command.HasNoFactBehavior && blocked.Count == 0
        };
    }

    /// <summary>
    /// Reports every reason a command has to keep its legacy productions or handler reference.
    /// </summary>
    /// <param name="context">The command being admitted.</param>
    /// <returns>True when any reason applies.</returns>
    bool ReportLegacyReasons(CommandAdmissionContext context)
    {
        var protectedValues = ReportProtectedValues(context);
        var requiredMapping = HasRequiredMapping(context);
        var preserveScenarios = ReportPreservedScenarios(context);
        var preserveExplicitSources = ReportPreservedExplicitSources(context);
        if (requiredMapping)
        {
            Report(context, "A required event payload mapping needs an unadmitted generated value; generated values and responses were left in code, and dependent productions were withheld rather than emitted incomplete");
        }

        return protectedValues || requiredMapping || preserveScenarios || preserveExplicitSources;
    }

    bool ReportProtectedValues(CommandAdmissionContext context)
    {
        if (!context.Authoring.Generated.Any(property => ReadsGenerated(context, property)))
        {
            return false;
        }

        Report(context, "Generated values are referenced by pre-generation property rules or requirements (PLAY0273); generated values and responses were left in code and the command retains its legacy productions or handler reference");
        return true;
    }

    bool ReportPreservedScenarios(CommandAdmissionContext context)
    {
        // A scenario whose only outcome is a response keeps nothing a fallback would preserve: it is emitted only
        // where its then returns fits the emitted response, and otherwise left out as before.
        var scenarios = SuccessfulScenarios(context);
        var needsFixtures = NeedsLegacyFixtures(context.Authoring, scenarios);
        if (needsFixtures)
        {
            Report(context, "Generation and responses were withheld to keep successful scenarios; Arc creates generated values inside Handle() with no seam a scenario can pin, so no deterministic generation fixture can be recovered and the command retains its legacy productions or handler reference");
        }

        var unrecoveredReturns = !needsFixtures && HasUnrecoveredReturns(context.Authoring, scenarios);
        if (unrecoveredReturns)
        {
            Report(context, "Responses were withheld to keep successful scenarios asserting CommandResult.Response in a way that cannot be stated as then returns, so the command retains its legacy productions or handler reference");
        }

        return needsFixtures || unrecoveredReturns;
    }

    bool ReportPreservedExplicitSources(CommandAdmissionContext context)
    {
        // A response-only scenario on a command with generated values is never emitted, so it preserves nothing.
        if (!MustPreserveExplicitSources(context.Authoring, context.Scope.Model, context.Command))
        {
            return false;
        }

        Report(context, $"Generation and responses for command '{context.Command.Name}' were withheld to keep scenarios with explicit given event sources; generated identity fixtures cannot be recovered from Arc scenarios, so the command retains its legacy productions or handler reference");
        return true;
    }

    void ReportDroppedIdentifier(CommandAdmissionContext context, CommandAuthoringModel retained)
    {
        if (context.Capped || !DroppedIdentifier(context, retained))
        {
            return;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult,
            $"The handler of command '{context.Command.Name}' yields an event source identifier or response alongside the event, but its generated identity is not admitted by ESM v7; no event source destination was inferred",
            $"{context.Location}.{context.Command.Name}");
    }

    List<ProducesModel> AdmitProductions(CommandAdmissionContext context, CommandAuthoringModel retained)
    {
        var productions = new List<ProducesModel>();
        foreach (var production in context.Command.Produces)
        {
            if (AdmitProduction(context, retained, production) is { } admitted)
            {
                productions.Add(admitted);
            }
        }

        return productions;
    }

    ProducesModel? AdmitProduction(CommandAdmissionContext context, CommandAuthoringModel retained, ProducesModel production)
    {
        if (OmissionReason(context, production) is { } reason)
        {
            Report(context, reason);
            return null;
        }

        var location = $"{context.Location}.{context.Command.Name}";
        var omitted = production.Mappings.Where(mapping => mapping.Source is PropertyPathSource source && context.Blocked.Any(name => Reads(source, name))).ToList();
        var pruned = CommandProductionPruning.WithoutMappings(production, omitted, context.Scope.Model, diagnostics, location);
        if (pruned is null)
        {
            return null;
        }

        WarnOmittedMappings(production, omitted, location);

        var mappings = pruned.Mappings.ToList();

        return pruned with
        {
            Mappings = mappings,
            CanInline = production.CanInline && !context.Legacy && mappings.Count == production.Mappings.Count(),
            UsesCommandContext = production.UsesCommandContext && !DroppedIdentifier(context, retained)
        };
    }

    void WarnOmittedMappings(ProducesModel production, List<PropertyMappingModel> omitted, string location)
    {
        foreach (var mapping in omitted)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableCommandProduction,
                $"The value given to '{production.EventName}.{mapping.Property}' is an unadmitted generated value rather than command input or a constant, so the mapping was left out and the production was retained",
                location);
        }
    }

    CommandAuthoringModel WithAdmittedRoute(CommandAdmissionContext context, CommandAuthoringModel retained, List<ProducesModel> productions)
    {
        if (context.Scope.AuthoringOnlyConstructs || retained.Route is not { } route)
        {
            return retained;
        }

        var reason = CommandRouteAdmission.Failure(route, context.Command with { Produces = productions }, context.Scope.Model, authoring: false);
        if (reason is null)
        {
            return retained;
        }

        diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandRoute, $"Command '{context.Command.Name}': {reason}; its entire route was left out", $"{context.Location}.{context.Command.Name}");
        return retained with { Route = null };
    }

    void Report(CommandAdmissionContext context, string reason) => Report(context.Command, context.Location, reason);

    void Report(CommandModel command, string location, string reason) => diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandResponse, reason, $"{location}.{command.Name}");
}
