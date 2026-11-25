// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.ManifestFile;
using Cratis.Arc.Availability.Suspensions;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ManifestFileBuilder;

public class when_building_from_a_running_engine : given.an_availability_host
{
    ManifestFileDocument _document;
    ManifestFileDocument _readBack;

    void Because()
    {
        var types = _root.GetRequiredService<ITypes>();
        _document = ManifestFileBuilder.Build((ArcAvailabilityGate)_gate, new DiscoveredParts(types), _root.GetRequiredService<AvailabilityCatalog>());
        _readBack = ManifestFileDocument.Parse(_document.ToJson());
    }

    [Fact] void should_name_the_format() => _readBack.Format.ShouldEqual(ManifestFileDocument.FormatName);
    [Fact] void should_name_the_version() => _readBack.Version.ShouldEqual(ManifestFileDocument.CurrentVersion);
    [Fact] void should_list_the_gated_namespaces() => _readBack.Scopes.Select(_ => _.Namespace).ShouldContainOnly("Cratis.Arc.Availability.given.Fleet", "Cratis.Arc.Availability.given.Fleet.Quality");
    [Fact] void should_list_every_command_and_read_model() => _readBack.Parts.Select(_ => _.Name).ShouldContain(typeof(PlusCommand).FullName, typeof(given.Fleet.Quality.ScoresReadModel).FullName, typeof(given.Fleet.Billing.InvoiceCommand).FullName);
    [Fact] void should_give_the_gate_declared_with_an_attribute() => _readBack.Parts.Single(_ => _.Name == typeof(PlusCommand).FullName).Gates.Single().ShouldEqual(new ManifestFor("Plus"));
    [Fact] void should_give_the_gates_down_the_path_outermost_first() => _readBack.Parts.Single(_ => _.Name == typeof(given.Fleet.Quality.ScoresReadModel).FullName).Gates.Count.ShouldEqual(2);
    [Fact] void should_say_what_kind_each_part_is() => _readBack.Parts.Single(_ => _.Name == typeof(given.Fleet.Quality.ScoresReadModel).FullName).Kind.ShouldEqual("readModel");
}
