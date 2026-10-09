// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

/// <summary>
/// A command reads a read model built beside it, which a slice in another scope declares - the one holding its keyed
/// query. A document of the command's scope declares the read model through the command's authoring-only
/// <c>reads</c>, beside the projection building it, so it must not import the declaration from elsewhere as well: a
/// document importing a name it declares does not compile cleanly.
/// </summary>
public class with_an_authoring_read_of_a_read_model_declared_in_another_scope : Specification
{
    const string ReadModel = "Invoice";
    const string ImportOfADeclaredName = "PLAY0290";
    const string Settling = $"{given.an_application.NestedFeature}.Settling";
    EmbeddedDocumentGeneration _generation;

    void Because() => Generate(1, true);

    void Generate(int localProjections, bool readHasProperties)
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
                    ReadModels = [new ReadModelModel(ReadModel, [reference]) { Namespace = Settling }],
                    Projections = localProjections == 0
                        ? [new ProjectionModel("InvoiceProjection", ReadModel, "event-log", ProjectionAutoMapMode.Enabled, false, ProjectionScopeModel.Empty with { From = [new(["InvoiceIssued"], "$eventSourceId", null, new Dictionary<string, string>())] })]
                        : []
                },
                Settling => slice with
                {
                    Commands =
                    [
                        new CommandModel("SettleInvoice", null, [reference], null, [], [new("InvoiceSettled", null, []) { UsesCommandContext = true }], null, null)
                        {
                            Authoring = new()
                            {
                                Reads = [new CommandReadModel(ReadModel, "invoice", "Reference") { Namespace = Settling, Properties = readHasProperties ? [reference] : [] }]
                            }
                        }
                    ],
                    Events = [new EventModel("InvoiceSettled", [reference], [])],
                    Projections = Enumerable.Range(0, localProjections).Select(index => new ProjectionModel(
                        $"InvoiceProjection{index}",
                        ReadModel,
                        "event-log",
                        ProjectionAutoMapMode.Enabled,
                        false,
                        ProjectionScopeModel.Empty with { From = [new(["InvoiceSettled"], "$eventSourceId", null, new Dictionary<string, string>())] })).ToList()
                },
                _ => slice
            }).ToList()
        };
        _generation = new EmbeddedDocumentGenerator().Generate(model, given.an_application.Options() with { AuthoringOnlyConstructs = true }, []);
    }

    GeneratedDocument DocumentOf(string id) => _generation.Documents.Single(_ => _.Document.Id == id);

    int Declarations(string source) => source.Split('\n').Count(_ => _.Trim() == $"readmodel {ReadModel}");

    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    public void should_not_import_a_read_that_scoped_authoring_admission_withholds(int localProjections, bool readHasProperties)
    {
        Generate(localProjections, readHasProperties);

        var source = DocumentOf(given.an_application.NestedFeature).Source;
        source.Split('\n').Select(_ => _.Trim()).ShouldNotContain($"import Library.Accounting.Invoices.Issuing.{ReadModel}");
        source.ShouldNotContain($"reads {ReadModel}");
        Declarations(source).ShouldEqual(0);
        _generation.IsSuccess.ShouldBeTrue();
        _generation.Documents.SelectMany(document => new ScreenplayCompiler().Compile(document.Source).Diagnostics).Select(diagnostic => diagnostic.Code).ShouldNotContain(ImportOfADeclaredName);
    }

    [Fact] void should_succeed() => _generation.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_import_it_into_the_reading_document() => DocumentOf(given.an_application.NestedFeature).Source.Split('\n').Select(_ => _.Trim()).ShouldNotContain($"import Library.Accounting.Invoices.Issuing.{ReadModel}");
    [Fact] void should_declare_it_once_in_the_reading_document() => Declarations(DocumentOf(given.an_application.NestedFeature).Source).ShouldEqual(1);
    [Fact] void should_declare_it_once_in_every_document() => _generation.Documents.All(_ => Declarations(_.Source) <= 1).ShouldBeTrue();
    [Fact] void should_compile_every_document() => _generation.Documents.All(_ => new ScreenplayCompiler().Compile(_.Source).Success).ShouldBeTrue();
    [Fact] void should_import_no_name_a_document_declares() => _generation.Documents.SelectMany(_ => new ScreenplayCompiler().Compile(_.Source).Diagnostics).Select(_ => _.Code).ShouldNotContain(ImportOfADeclaredName);
}
