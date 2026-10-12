// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_positional_enumeration_document : a_generated_document
{
    protected const string Source = """
        using Cratis.Chronicle.Events;
        namespace Library.Orders.Placing;
        public enum Status { Draft = 0, Issued = 1 }
        [EventType] public record OrderPlaced(Status Status);
        """;

    protected void AssertPositionalDocument()
    {
        AssertDocument();
        Result.Source.ShouldContain("draft = 0\n  issued = 1");
        Bound.Value!.Model.Application.Concepts.Single().Numbers.ShouldBeNull();
        SemanticVersion.V11.IsAtLeast(Bound.Value.Model.SemanticVersion).ShouldBeTrue();
        var bare = Result.Source.Replace("draft = 0", "draft", StringComparison.Ordinal).Replace("issued = 1", "issued", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Library"));
        const string Key = "application";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "application.play", bare);
        var bound = new SemanticModelCompiler().Compile("Library", SemanticDocumentSet.Create([document], catalog));
        bound.Success.ShouldBeTrue();
        bound.Value!.Model.SemanticVersion.ShouldEqual(Bound.Value.Model.SemanticVersion);
        bound.Value.Model.Revision.ShouldEqual(Bound.Value.Model.Revision);
    }
}
