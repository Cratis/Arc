// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Embedded.Hosting;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_serving_generated_documents;

/// <summary>
/// A host viewing an application that was built without embedded documents generates them itself and serves them
/// from memory. They have to read exactly as the embedded ones do - the same catalog, the same sources, the same
/// models - or the viewer would describe the same application two different ways.
/// </summary>
public class from_memory : Specification
{
    EmbeddedDocumentGeneration _generation;
    EventModelExplorer _explorer;

    void Establish() => _generation = new EmbeddedDocumentGenerator()
        .Generate(given.an_application.Build(), given.an_application.Options(), []);

    void Because() => _explorer = new EventModelExplorer(EventModelCatalog.For([_generation.ToResources(given.an_application.AssemblyName)]));

    [Fact] void should_hold_one_project() => _explorer.Hierarchy.Count.ShouldEqual(1);

    [Fact] void should_name_the_project_after_the_assembly() => _explorer.Hierarchy[0].Id.ShouldEqual(given.an_application.AssemblyName);

    [Fact] void should_hold_every_generated_document() =>
        _explorer.Hierarchy[0].Documents.Select(_ => _.Id).ShouldContainOnly(_generation.Documents.Select(_ => _.Document.Id));

    [Fact] void should_serve_the_generated_source_of_a_document() =>
        (_explorer.TryGetSource(given.an_application.AssemblyName, given.an_application.Module, out var source) &&
            source == _generation.Documents.First(_ => _.Document.Id == given.an_application.Module).Source).ShouldBeTrue();

    [Fact] void should_compile_every_document_to_a_model() =>
        _generation.Documents.All(_ =>
            _explorer.TryGetModel(given.an_application.AssemblyName, _.Document.Id, out var model) && model.Success).ShouldBeTrue();
}
