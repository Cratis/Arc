// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.Embedded.for_EmbeddedBuild;

public class when_consuming_a_compiled_assembly : Specification
{
    Assembly _assembly;
    EmbeddedDocumentCatalog _catalog;
    Dictionary<string, string> _documents;
    EventModelView _model;

    void Establish() => _assembly = typeof(Company.Library.Program).Assembly;

    void Because()
    {
        using var catalogStream = _assembly.GetManifestResourceStream(EmbeddedResourceNames.Catalog)!;
        using var catalogReader = new StreamReader(catalogStream);
        _catalog = EmbeddedDocumentCatalog.Deserialize(catalogReader.ReadToEnd());
        _documents = _catalog.Documents.ToDictionary(_ => _.Id, ReadDocument, StringComparer.Ordinal);
        _model = new EventModelParser().Parse("Company.Library", "Library", _documents["Company.Library"]);
    }

    [Fact] void should_compile_with_configured_features() => Company.Library.CompilerFeatureCalls.Intercepted().ShouldEqual("Current:forwarded|Legacy:forwarded");
    [Fact] void should_embed_five_documents() => _catalog.Documents.Count.ShouldEqual(5);
    [Fact] void should_embed_an_assembly_document_at_the_root_namespace() => _catalog.Documents.Single(_ => _.Id == "Company.Library").Kind.ShouldEqual(EmbeddedDocumentKind.Assembly);
    [Fact] void should_classify_catalog_as_a_module() => _catalog.Documents.Single(_ => _.Id == "Company.Library.Catalog").Kind.ShouldEqual(EmbeddedDocumentKind.Module);
    [Fact] void should_declare_catalog_as_a_module_in_the_full_document() => _documents["Company.Library"].Contains("module Catalog", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_classify_accounts_as_a_rooted_feature() => _catalog.Documents.Single(_ => _.Id == "Company.Library.Accounts").Kind.ShouldEqual(EmbeddedDocumentKind.Feature);
    [Fact] void should_place_the_rooted_feature_under_the_assembly() => _catalog.Documents.Single(_ => _.Id == "Company.Library.Accounts").ParentId.ShouldEqual("Company.Library");
    [Fact] void should_place_authors_under_catalog() => _catalog.Documents.Single(_ => _.Id == "Company.Library.Catalog.Authors").ParentId.ShouldEqual("Company.Library.Catalog");
    [Fact] void should_include_all_slices_in_the_assembly_document() => new[] { "RegisterAuthor", "All", "SignIn" }.All(_ => _documents["Company.Library"].Contains(_, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_embed_the_emitted_event_declaration() => _documents["Company.Library"].Contains("event AuthorRegistered", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_carry_the_event_into_the_renderable_board_model() => _model.EventModel!.Collections
        .SelectMany(_ => _.Modules).SelectMany(_ => _.Features).SelectMany(_ => _.Slices)
        .SelectMany(_ => _.Events).Any(_ => _.Name == "AuthorRegistered").ShouldBeTrue();
    [Fact] void should_keep_other_features_out_of_the_authors_document() => _documents["Company.Library.Catalog.Authors"].Contains("SignIn", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_embed_documents_accepted_by_the_screenplay_compiler() => _documents.Values.All(_ => new ScreenplayCompiler().Compile(_).Success).ShouldBeTrue();
    [Fact] void should_embed_documents_without_compiler_warnings() => _documents.Values.SelectMany(_ => new ScreenplayCompiler().Compile(_).Diagnostics)
        .Where(_ => _.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Warning).ShouldBeEmpty();
    [Fact] void should_use_actual_manifest_resources_for_every_document() => _catalog.Documents.All(_ => _assembly.GetManifestResourceNames().Contains(_.ResourceName)).ShouldBeTrue();

    string ReadDocument(EmbeddedDocument document)
    {
        using var stream = _assembly.GetManifestResourceStream(document.ResourceName)!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
