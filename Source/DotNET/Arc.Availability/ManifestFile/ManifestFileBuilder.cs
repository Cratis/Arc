// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Suspensions;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Slices;

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Builds the <see cref="ManifestFileDocument"/> of a running engine: the complete picture, including gates registered in code.
/// </summary>
public static class ManifestFileBuilder
{
    /// <summary>
    /// Builds the manifest file.
    /// </summary>
    /// <param name="gate">The <see cref="ArcAvailabilityGate"/> whose gates are described.</param>
    /// <param name="discovered">The <see cref="DiscoveredParts"/>.</param>
    /// <param name="catalog">The <see cref="AvailabilityCatalog"/> that defines the blocks.</param>
    /// <param name="manifest">The <see cref="AvailabilityManifest"/> that gives the audiences and context values; none when it is not available.</param>
    /// <returns>The <see cref="ManifestFileDocument"/>.</returns>
    public static ManifestFileDocument Build(ArcAvailabilityGate gate, DiscoveredParts discovered, AvailabilityCatalog catalog, AvailabilityManifest? manifest = null)
    {
        var blocks = catalog.Definitions
            .Select(_ => new ManifestBlock(_.Name, _.Expression.ToManifest(), _.Description))
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ToList();
        var scopes = gate.Scopes
            .Select(_ => new ManifestScope(_.Namespace, _.Gate.ToManifest()))
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ToList();
        var parts = gate.GatedParts.Concat(discovered.All).Distinct()
            .Select(part => ToPart(part, gate))
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ToList();

        var audiences = (manifest?.Audiences ?? []).Select(_ => new ManifestAudience(_.Name.Value, _.Description)).ToList();
        var contextValues = (manifest?.ContextValues ?? [])
            .Select(_ => new ManifestContextValue(_.Name, string.Join('/', _.Scope.Path), _.Required, _.Origin))
            .ToList();

        var gates = blocks.Select(_ => _.Gate).Concat(scopes.Select(_ => _.Gate)).Concat(parts.SelectMany(_ => _.Gates));
        var conditions = gates
            .SelectMany(NamedConditions)
            .DistinctBy(_ => _.Name)
            .Select(_ => new ManifestNamedConditionEntry(_.Name, _.Description))
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ToList();

        var suspendable = discovered.Entries().Select(_ => new ManifestSuspendable(_.Kind.ToLowerInvariant(), _.Path, _.Name)).ToList();
        return new(
            ManifestFileDocument.FormatName,
            ManifestFileDocument.CurrentVersion,
            manifest?.Origin ?? string.Empty,
            audiences,
            blocks,
            conditions,
            contextValues,
            scopes,
            parts,
            suspendable);
    }

    static ManifestPart ToPart(Type part, ArcAvailabilityGate gate)
    {
        var location = SliceLocation.For(part);
        var kind = part.IsDefined(typeof(CommandAttribute), false) ? "command" : "readModel";
        return new(
            part.FullName ?? part.Name,
            kind,
            location.Module.Value,
            location.Feature.Value,
            location.Slice.Value,
            [.. gate.GatesOf(part).Select(_ => _.ToManifest())]);
    }

    static IEnumerable<ManifestNamedCondition> NamedConditions(ManifestExpression expression) => expression switch
    {
        ManifestWhen w => NamedConditions(w.Condition),
        ManifestAll a => a.Items.SelectMany(NamedConditions),
        ManifestAny a => a.Items.SelectMany(NamedConditions),
        _ => []
    };

    static IEnumerable<ManifestNamedCondition> NamedConditions(ManifestCondition condition) => condition switch
    {
        ManifestNamedCondition n => [n],
        ManifestAllConditions a => a.Items.SelectMany(NamedConditions),
        ManifestAnyConditions a => a.Items.SelectMany(NamedConditions),
        _ => []
    };
}
