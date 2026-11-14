// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.for_EventModelParser.given;
using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.for_EventModelCatalog.when_reading_in_memory_resources;

public class with_document_origins : a_document_with_an_unpopulated_field
{
    EventModelCatalog _catalog;
    CompiledEventModels _models;
    EventModelView _legacy;
    EventModelView _embedded;
    EventModelView _generated;
    EventModelView _cached;

    void Establish()
    {
        var resources = new InMemoryEventModelResources("Records", new Dictionary<string, string>
        {
            [EventModelCatalog.ResourceName] =
                """
                {"documents":[
                  {"id":"Legacy","kind":"assembly","resourceName":"legacy.play"},
                  {"id":"Embedded","kind":"assembly","resourceName":"embedded.play","origin":"embedded"},
                  {"id":"Generated","kind":"assembly","resourceName":"generated.play","origin":"GeNeRaTeD"}
                ]}
                """,
            ["legacy.play"] = Source,
            ["embedded.play"] = Source,
            ["generated.play"] = Source
        });
        _catalog = EventModelCatalog.For([resources]);
        _models = new CompiledEventModels(_catalog);
    }

    void Because()
    {
        _models.TryGet("Records", "Legacy", out _legacy);
        _models.TryGet("Records", "Embedded", out _embedded);
        _models.TryGet("Records", "Generated", out _generated);
        _models.TryGet("Records", "Generated", out _cached);
    }

    [Fact] void should_default_unmarked_documents_to_embedded() => _catalog.Projects.Single().Documents.Single(_ => _.Id == "Legacy").Origin.ShouldEqual(EventModelOrigin.Embedded);
    [Fact] void should_read_generated_origins_case_insensitively() => _catalog.Projects.Single().Documents.Single(_ => _.Id == "Generated").Origin.ShouldEqual(EventModelOrigin.Generated);
    [Fact] void should_refuse_the_unmarked_document() => _legacy.Success.ShouldBeFalse();
    [Fact] void should_refuse_the_explicitly_embedded_document() => _embedded.Success.ShouldBeFalse();
    [Fact] void should_draw_the_generated_document() => _generated.Success.ShouldBeTrue();
    [Fact] void should_pass_generated_coverage_to_the_warning_channel() => _generated.Warnings.Select(_ => _.Code).ShouldContain("PLAY0284");
    [Fact] void should_keep_the_cached_view() => ReferenceEquals(_cached, _generated).ShouldBeTrue();
}
