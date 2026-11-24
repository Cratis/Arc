// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Slices;
using Cratis.Types;

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// The parts of the application an operator can suspend, found from the types: nothing has to be declared.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> to discover the parts from.</param>
public class DiscoveredParts(ITypes types)
{
    readonly Lazy<IReadOnlyList<Type>> _all = new(() => Find(types));

    /// <summary>
    /// Gets every discovered part: the model-bound commands and read models.
    /// </summary>
    public IReadOnlyList<Type> All => _all.Value;

    /// <summary>
    /// Gets an entry for every discovered module, feature, slice and part, so an operator can pick one to suspend.
    /// </summary>
    /// <returns>The entries, ordered by path.</returns>
    public IReadOnlyList<PartEntry> Entries()
    {
        var entries = new Dictionary<string, PartEntry>();
        foreach (var part in All)
        {
            var location = SliceLocation.For(part);
            var segments = new List<string>();
            AddLevel(entries, segments, location.Module.Value, PartEntry.ModuleKind);
            AddLevel(entries, segments, location.Feature.Value, PartEntry.FeatureKind);
            AddLevel(entries, segments, location.Slice.Value, PartEntry.SliceKind);
            var path = PartPaths.For(part);
            entries[path] = new PartEntry(PartEntry.PartKind, path, part.Name);
        }

        return [.. entries.Values.OrderBy(_ => _.Path, StringComparer.Ordinal)];
    }

    static IReadOnlyList<Type> Find(ITypes types) =>
        [.. types.All
            .Where(type => (type.IsClass || type.IsValueType) &&
                (type.IsDefined(typeof(CommandAttribute), false) || type.IsDefined(typeof(ReadModelAttribute), false)))
            .Distinct()];

    static void AddLevel(Dictionary<string, PartEntry> entries, List<string> segments, string name, string kind)
    {
        if (name.Length == 0)
        {
            return;
        }

        segments.Add(name);
        var path = string.Join('/', segments);
        entries.TryAdd(path, new PartEntry(kind, path, name));
    }
}
