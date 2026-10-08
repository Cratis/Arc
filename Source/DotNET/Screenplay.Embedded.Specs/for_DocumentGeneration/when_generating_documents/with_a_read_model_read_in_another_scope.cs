// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

/// <summary>
/// A read model is declared once, in the slice that owns it, and a query in another part of the application can still
/// answer with it. Once that part is a document of its own, the declaration is somewhere else, and the dependency is
/// stated the way one on an event declared elsewhere is.
/// </summary>
public class with_a_read_model_read_in_another_scope : Specification
{
    const string ReadModel = "Invoice";
    EmbeddedDocumentGeneration _generation;

    void Because()
    {
        var reference = new PropertyModel("Reference", new("String", false, false));
        var model = given.an_application.Build();
        model = model with
        {
            Slices = model.Slices.Select(slice => slice.Namespace switch
            {
                $"{given.an_application.Feature}.Issuing" => slice with
                {
                    Queries = [new QueryModel("InvoiceByReference", new(ReadModel, false, true), reference, [], null)],
                    ReadModels = [new ReadModelModel(ReadModel, [reference]) { Namespace = slice.Namespace }]
                },
                $"{given.an_application.NestedFeature}.Settling" => slice with
                {
                    Queries = [new QueryModel("AllInvoices", new(ReadModel, true, false), null, [], null)]
                },
                _ => slice
            }).ToList()
        };
        _generation = new EmbeddedDocumentGenerator().Generate(model, given.an_application.Options(), []);
    }

    GeneratedDocument DocumentOf(string id) => _generation.Documents.Single(_ => _.Document.Id == id);

    [Fact] void should_succeed() => _generation.IsSuccess.ShouldBeTrue();
    [Fact] void should_declare_it_in_the_assembly_document() => DocumentOf(given.an_application.RootNamespace).Source.ShouldContain($"readmodel {ReadModel}");
    [Fact] void should_declare_it_in_the_owning_feature_document() => DocumentOf(given.an_application.Feature).Source.ShouldContain($"readmodel {ReadModel}");
    [Fact] void should_import_it_into_the_reading_document() => DocumentOf(given.an_application.NestedFeature).Source.ShouldContain($"import Library.Accounting.Invoices.Issuing.{ReadModel}");
    [Fact] void should_not_declare_it_in_the_reading_document() => DocumentOf(given.an_application.NestedFeature).Source.ShouldNotContain($"readmodel {ReadModel}");
    [Fact] void should_compile_every_document_independently() => _generation.Documents.All(_ => new ScreenplayCompiler().Compile(_.Source).Success).ShouldBeTrue();
}
