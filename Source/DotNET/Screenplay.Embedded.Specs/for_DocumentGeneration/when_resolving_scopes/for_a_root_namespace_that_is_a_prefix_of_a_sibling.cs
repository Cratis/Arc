// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_resolving_scopes;

/// <summary>
/// A root namespace is stripped on whole segments. <c>Library.Authorship</c> does not sit inside
/// <c>Library.Author</c> however much of its text matches, and a prefix compared as characters would reparent a
/// whole feature under a namespace it has nothing to do with.
/// </summary>
public class for_a_root_namespace_that_is_a_prefix_of_a_sibling : Specification
{
    const string Root = "Library.Author";

    static readonly ApplicationModel _model = new(
        "Library.Author",
        "Library.Author",
        [],
        [],
        [
            SliceModel.Empty("Library.Author.Registration.Registering", "Registering", SliceKind.StateChange),
            SliceModel.Empty("Library.Authorship.Claims.Claiming", "Claiming", SliceKind.StateChange)
        ],
        []);

    IReadOnlyList<DocumentScope> _scopes;

    void Because() => _scopes = DocumentScopes.Of(_model, new EmbeddedDocumentOptions("Library.Author", Root));

    [Fact] void should_resolve_a_scope_for_the_namespace_really_beneath_the_root() =>
        _scopes.Select(_ => _.Namespace).ShouldContain("Library.Author.Registration");

    [Fact] void should_not_resolve_a_scope_for_a_namespace_that_merely_starts_with_the_same_text() =>
        _scopes.Select(_ => _.Namespace).ShouldNotContain("Library.Authorship.Claims");

    [Fact] void should_resolve_nothing_but_the_assembly_and_the_one_namespace_beneath_it() => _scopes.Count.ShouldEqual(2);

    [Fact] void should_skip_every_segment_of_the_root_but_the_last() => _scopes[0].SegmentsToSkip.ShouldEqual(1);

    [Fact] void should_declare_the_root_document_within_the_last_segment_of_the_root_namespace() =>
        _scopes[0].ModuleName.ShouldEqual("Author");

    [Fact] void should_not_place_a_namespace_outside_the_root_within_a_scope() =>
        Namespaces.IsWithin("Library.Authorship.Claims.Claiming", Root).ShouldBeFalse();
}
