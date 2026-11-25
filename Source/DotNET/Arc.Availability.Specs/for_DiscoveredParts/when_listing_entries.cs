// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Slices;
using Cratis.Types;

namespace Cratis.Arc.Availability.Suspensions.for_DiscoveredParts;

public class when_listing_entries : Specification
{
    IReadOnlyList<PartEntry> _entries;
    IReadOnlyList<Type> _parts;
    SliceLocation _location;

    void Because()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(ACommand), typeof(AReadModel), typeof(NotAPart)]);
        var discovered = new DiscoveredParts(types);
        _parts = discovered.All;
        _entries = discovered.Entries();
        _location = SliceLocation.For(typeof(ACommand));
    }

    [Fact] void should_discover_commands_and_read_models_without_any_declaration() => _parts.ShouldContainOnly(typeof(ACommand), typeof(AReadModel));
    [Fact] void should_list_the_module() => _entries.ShouldContain(new PartEntry(PartEntry.ModuleKind, _location.Module.Value, _location.Module.Value));
    [Fact] void should_list_the_feature() => _entries.ShouldContain(new PartEntry(PartEntry.FeatureKind, $"{_location.Module.Value}/{_location.Feature.Value}", _location.Feature.Value));
    [Fact] void should_list_the_slice() => _entries.ShouldContain(new PartEntry(PartEntry.SliceKind, $"{_location.Module.Value}/{_location.Feature.Value}/{_location.Slice.Value}", _location.Slice.Value));
    [Fact] void should_list_each_part() => _entries.Where(_ => _.Kind == PartEntry.PartKind).Select(_ => _.Path).ShouldContainOnly(PartPaths.For(typeof(ACommand)), PartPaths.For(typeof(AReadModel)));
    [Fact] void should_list_each_level_once() => _entries.Count(_ => _.Kind != PartEntry.PartKind).ShouldEqual(3);

    [Command]
    public record ACommand;

    [ReadModel]
    public record AReadModel;

    public record NotAPart;
}
