// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class with_an_inline_event_imported_by_another_scope : Specification
{
    const string EventIdentity = "Library:Library.Authors.Registration.AuthorRegistered";
    EmbeddedDocumentGeneration _generation;

    void Because()
    {
        var model = given.an_application.Build();
        var registration = given.an_application.Registration();
        var declaration = registration.Events.Single() with { TypeIdentity = EventIdentity, CanInline = true };
        var command = registration.Commands.Single() with
        {
            Identifier = "Id",
            Properties = [new("Id", new("Uuid", false, false)), .. registration.Commands.Single().Properties],
            Produces = [new(given.an_application.EventOfTheRootedFeature, null, [new("Reference", new PropertyPathSource("Reference"))])
            {
                EventTypeIdentity = EventIdentity,
                CanInline = true,
                UsesCommandContext = true
            }]
        };
        registration = registration with { Commands = [command], Events = [declaration] };
        model = model with
        {
            Slices = model.Slices.Select(_ => _.Namespace == registration.Namespace ? registration : _).ToList(),
            EventProducerCounts = new Dictionary<string, int>(StringComparer.Ordinal) { [EventIdentity] = 1 }
        };
        _generation = new EmbeddedDocumentGenerator().Generate(model, given.an_application.Options(), []);
    }

    GeneratedDocument DocumentOf(string id) => _generation.Documents.Single(_ => _.Document.Id == id);

    [Fact] void should_succeed() => _generation.IsSuccess.ShouldBeTrue();
    [Fact] void should_declare_inline_in_the_assembly_document() => DocumentOf(given.an_application.RootNamespace).Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_declare_inline_in_the_producing_feature_document() => DocumentOf(given.an_application.RootedFeature).Source.ShouldContain("produces event AuthorRegistered");
    [Fact] void should_import_the_inline_event_into_the_consuming_document() => DocumentOf(given.an_application.NestedFeature).Source.ShouldContain("import Library.Authors.Registration.AuthorRegistered");
    [Fact] void should_not_duplicate_the_declaration_in_the_consuming_document() => DocumentOf(given.an_application.NestedFeature).Source.ShouldNotContain("event AuthorRegistered");
    [Fact] void should_compile_every_document_independently() => _generation.Documents.All(_ => new ScreenplayCompiler().Compile(_.Source).Success).ShouldBeTrue();
}
