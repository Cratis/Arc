// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Withholds compliance annotations only from concepts occupying protected identity positions in the emitted syntax.
/// </summary>
/// <param name="application">The assembled document whose positions Screenplay will validate.</param>
internal sealed class ProtectedIdentityAnnotations(ApplicationSyntax application)
{
    readonly AuthoringProductionResolver _productions = new(application);
    readonly ILookup<string, TypeSyntax> _types = (application.Types ?? []).ToLookup(type => type.Name, StringComparer.Ordinal);
    readonly HashSet<string> _protected = application.Concepts.Where(concept => concept.AttributeNames.Any(IsProtection)).Select(concept => concept.Name).ToHashSet(StringComparer.Ordinal);
    readonly Dictionary<string, HashSet<string>> _uses = new(StringComparer.Ordinal);

    /// <summary>
    /// Removes pii and secret annotations rejected at emitted identity positions.
    /// </summary>
    /// <param name="diagnostics">Where the retained uses and withheld annotations are reported.</param>
    /// <returns>The document with only unsupported protection annotations removed.</returns>
    internal ApplicationSyntax Apply(ScreenplayDiagnostics diagnostics)
    {
        CollectEventSources();
        var events = new AuthoringProductionResolver(_productions.Declarations.Where(declaration => declaration.Kind == AuthoringProductionKind.Event));
        foreach (var (slice, scope) in application.Modules.SelectMany(module => Slices(module.Features, [module.Name])))
        {
            foreach (var command in slice.Commands)
            {
                CollectCommand(command, slice);
            }

            foreach (var reaction in slice.Reactions)
            {
                CollectReaction(reaction, slice, scope, events);
            }
        }

        return application with { Concepts = application.Concepts.Select(concept => Withhold(concept, diagnostics)).ToList() };
    }

    static bool IsProtection(string attribute) => attribute == ConceptAttributeSyntax.Pii || attribute == ConceptAttributeSyntax.Sensitive;

    static IEnumerable<PropertySyntax> TriggerProperties(IEnumerable<TriggerDataSyntax> data) => data
        .Where(datum => datum.Type is not null).Select(datum => new PropertySyntax(datum.Name, datum.Type!, datum.Location));

    static IEnumerable<(SliceSyntax Slice, IReadOnlyList<string> Scope)> Slices(IEnumerable<FeatureSyntax> features, IReadOnlyList<string> outer)
    {
        foreach (var feature in features)
        {
            string[] scope = [.. outer, feature.Name];
            foreach (var slice in feature.Slices)
            {
                yield return (slice, [.. scope, slice.Name]);
            }

            foreach (var nested in Slices(feature.Features, scope))
            {
                yield return nested;
            }
        }
    }

    void CollectEventSources()
    {
        foreach (var source in application.EventSources)
        {
            Add(source.Identifier, $"identifier of event source '{source.Name}'");
            foreach (var stream in source.Streams)
            {
                Add(stream.StreamId, $"stream id of '{source.Name}.{stream.Name}'");
                foreach (var part in stream.StreamIdParts)
                {
                    Add(part.Type, $"stream id part '{part.Name}' of '{source.Name}.{stream.Name}'");
                }
            }
        }
    }

    void CollectCommand(CommandSyntax command, SliceSyntax slice)
    {
        foreach (var property in command.Properties.Where(property => property.IsIdentifier))
        {
            Add(property.Type, $"identifier of command '{command.Name}'");
        }

        var routes = (command.Stream is { } selected ? new[] { selected }.Concat(command.StreamCandidates) : command.StreamCandidates)
            .Concat(command.Produces.Select(produced => produced.Stream).OfType<CommandStreamSyntax>())
            .Where(route => route.PropertyCandidate is null);
        foreach (var route in routes)
        {
            if (route.StreamId is { } mapping)
            {
                CollectMapping(command, route, mapping, "stream id");
            }

            foreach (var part in route.StreamIdParts)
            {
                CollectMapping(command, route, part, $"stream id part '{part.Property}'");
            }
        }

        // Use the language's event/operation discriminator, including inline operations and mixed-kind ambiguity.
        foreach (var production in command.Produces.Where(production => _productions.IsEventProduction(production, slice)))
        {
            if (production.For is PathExpressionSyntax path && Property(command.Properties, path.Path) is { IsIdentifier: false } property)
            {
                Add(property.Type, $"for destination of '{production.Event}' in command '{command.Name}'");
            }
        }
    }

    void CollectMapping(CommandSyntax command, CommandStreamSyntax route, PropertyMappingSyntax mapping, string position)
    {
        if (mapping.Source is PathExpressionSyntax path && Property(command.Properties, path.Path) is { } property)
        {
            Add(property.Type, $"{position} route mapping of '{route.EventSource}.{route.Stream}' in command '{command.Name}'");
        }
    }

    void CollectReaction(ReactionSyntax reaction, SliceSyntax slice, IReadOnlyList<string> scope, AuthoringProductionResolver events)
    {
        foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Source is NamedTriggerSourceSyntax))
        {
            var name = ((NamedTriggerSourceSyntax)trigger.Source).Name;
            var resolution = events.Resolve(name, scope);
            if (resolution.Kind == AuthoringProductionKind.Unresolved && application.Imports.Where(import => import.Name == name).ToArray() is [var imported])
            {
                resolution = events.Resolve(imported.QualifiedName, scope);
            }

            var resolvedEvent = resolution.Declaration?.Node as EventSyntax;
            var shapes = new List<IEnumerable<PropertySyntax>?> { resolvedEvent?.Properties, TriggerProperties(trigger.Data) };
            if (resolvedEvent is null && !application.Imports.Any(import => import.Name == name))
            {
                shapes.AddRange((application.Triggers ?? []).Where(declared => declared.Name == name).Select(declared => TriggerProperties(declared.Data)));
            }

            var productions = (trigger.Produces ?? []).Concat((trigger.Invokes ?? []).SelectMany(invocation => invocation.OnRefused).SelectMany(branch => branch.Produces));
            foreach (var production in productions.Where(production => _productions.IsEventProduction(production, slice)))
            {
                if (production.For is PathExpressionSyntax path)
                {
                    var type = shapes.Select(properties => Property(properties, path.Path)?.Type).FirstOrDefault(type => type is not null && _protected.Contains(type.Name));
                    Add(type, $"for destination of '{production.Event}' in reaction '{reaction.Name}' on '{name}'");
                }
            }
        }
    }

    /// <summary>
    /// Mirrors ConsistencyDeclarations.Property: unique local members, then unique declared composite types.
    /// </summary>
    /// <param name="properties">The containing declaration's properties.</param>
    /// <param name="path">The path through those properties.</param>
    /// <returns>The resolved property, or null for unknown or ambiguous paths.</returns>
    PropertySyntax? Property(IEnumerable<PropertySyntax>? properties, string path)
    {
        var segments = path.Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            if (properties?.Where(property => property.Name == segments[index]).ToArray() is not [var property])
            {
                return null;
            }

            if (index == segments.Length - 1)
            {
                return property;
            }

            properties = _types[property.Type.Name].ToArray() is [var type] ? type.Properties : null;
        }

        return null;
    }

    void Add(TypeRefSyntax? type, string position)
    {
        if (type is null || !_protected.Contains(type.Name))
        {
            return;
        }

        if (!_uses.TryGetValue(type.Name, out var uses))
        {
            uses = new(StringComparer.Ordinal);
            _uses[type.Name] = uses;
        }

        uses.Add(position);
    }

    ConceptSyntax Withhold(ConceptSyntax concept, ScreenplayDiagnostics diagnostics)
    {
        if (!_uses.TryGetValue(concept.Name, out var uses))
        {
            return concept;
        }

        var attributes = concept.AttributeNames.Where(IsProtection).Select(attribute => attribute == ConceptAttributeSyntax.Pii ? "pii" : "secret");
        diagnostics.Information(
            ScreenplayDiagnosticCodes.ProtectedIdentityAnnotation,
            $"Concept '{concept.Name}' is used as {string.Join(", ", uses.Order(StringComparer.Ordinal))}; its {string.Join('/', attributes)} annotations were left off because Screenplay rejects protected identities (PLAY0515)",
            concept.Name);

        return concept with { Attributes = concept.Attributes.Where(attribute => !IsProtection(attribute.Name)).ToList() };
    }
}
