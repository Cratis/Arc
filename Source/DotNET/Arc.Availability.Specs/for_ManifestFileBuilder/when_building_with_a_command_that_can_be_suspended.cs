// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.ManifestFile;
using Cratis.Arc.Availability.Suspensions;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ManifestFileBuilder;

public class when_building_with_a_command_that_can_be_suspended : given.an_availability_host
{
    ManifestFileDocument _document;

    protected override IEnumerable<Type> Types => [typeof(LooseCommand)];

    void Because() =>
        _document = ManifestFileBuilder.Build((ArcAvailabilityGate)_gate, new DiscoveredParts(_root.GetRequiredService<ITypes>()), _root.GetRequiredService<AvailabilityCatalog>());

    [Fact] void should_list_the_command_as_a_command() => _document.Parts.Single().Kind.ShouldEqual("command");
    [Fact] void should_list_the_command_as_suspendable() => _document.Suspendable.Single(_ => _.Kind == "part").Name.ShouldEqual(nameof(LooseCommand));
    [Fact] void should_name_the_path_of_the_command() => _document.Suspendable.Single(_ => _.Kind == "part").Path.ShouldEqual(PartPaths.For(typeof(LooseCommand)));

    [Command]
    public record LooseCommand;
}
