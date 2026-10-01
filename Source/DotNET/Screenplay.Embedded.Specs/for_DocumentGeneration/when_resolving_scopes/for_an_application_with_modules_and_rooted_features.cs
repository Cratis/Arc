// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_resolving_scopes;

/// <summary>
/// The hierarchy is what an application already wrote, read off its namespaces. A first segment with namespaces
/// between it and its slices is a module; one whose children are only slices is a feature that happens to sit at
/// the top, and inventing a module around it would put a name in the navigation that nobody wrote.
/// </summary>
public class for_an_application_with_modules_and_rooted_features : Specification
{
    IReadOnlyList<DocumentScope> _scopes;

    void Because() => _scopes = DocumentScopes.Of(given.an_application.Build(), given.an_application.Options());

    DocumentScope ScopeOf(string @namespace) => _scopes.First(_ => _.Namespace == @namespace);

    [Fact] void should_resolve_one_scope_per_part_of_the_application() => _scopes.Count.ShouldEqual(5);

    [Fact] void should_root_the_hierarchy_at_the_root_namespace() =>
        _scopes[0].Id.ShouldEqual(given.an_application.RootNamespace);

    [Fact] void should_describe_the_root_as_the_assembly() =>
        _scopes[0].Kind.ShouldEqual(EmbeddedDocumentKind.Assembly);

    [Fact] void should_leave_the_root_without_a_parent() => _scopes[0].ParentId.ShouldBeNull();

    [Fact] void should_name_the_root_document_after_the_assembly() =>
        _scopes[0].Title.ShouldEqual(given.an_application.AssemblyName);

    [Fact] void should_treat_a_namespace_with_features_beneath_it_as_a_module() =>
        ScopeOf(given.an_application.Module).Kind.ShouldEqual(EmbeddedDocumentKind.Module);

    [Fact] void should_place_the_module_under_the_assembly() =>
        ScopeOf(given.an_application.Module).ParentId.ShouldEqual(given.an_application.RootNamespace);

    [Fact] void should_treat_a_namespace_whose_children_are_only_slices_as_a_feature() =>
        ScopeOf(given.an_application.RootedFeature).Kind.ShouldEqual(EmbeddedDocumentKind.Feature);

    [Fact] void should_place_the_rooted_feature_under_the_assembly_rather_than_under_an_invented_module() =>
        ScopeOf(given.an_application.RootedFeature).ParentId.ShouldEqual(given.an_application.RootNamespace);

    [Fact] void should_resolve_the_feature_of_the_module() =>
        ScopeOf(given.an_application.Feature).Kind.ShouldEqual(EmbeddedDocumentKind.Feature);

    [Fact] void should_place_the_feature_under_its_module() =>
        ScopeOf(given.an_application.Feature).ParentId.ShouldEqual(given.an_application.Module);

    [Fact] void should_resolve_the_feature_nested_in_a_feature() =>
        ScopeOf(given.an_application.NestedFeature).Kind.ShouldEqual(EmbeddedDocumentKind.Feature);

    [Fact] void should_place_the_nested_feature_under_the_feature_holding_it() =>
        ScopeOf(given.an_application.NestedFeature).ParentId.ShouldEqual(given.an_application.Feature);

    [Fact] void should_name_a_scope_after_the_last_segment_of_its_namespace() =>
        ScopeOf(given.an_application.NestedFeature).Title.ShouldEqual("Payments");

    [Fact] void should_not_resolve_a_scope_for_a_slice_sitting_in_the_root_namespace() =>
        _scopes.Any(_ => _.Namespace.EndsWith("Housekeeping", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact] void should_not_resolve_a_scope_for_a_slice_sitting_directly_in_a_module() =>
        _scopes.Any(_ => _.Namespace.EndsWith("Reporting", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact] void should_order_every_parent_before_the_scopes_beneath_it() =>
        Enumerable.Range(1, _scopes.Count - 1)
            .All(index => _scopes.Take(index).Any(_ => _.Id == _scopes[index].ParentId))
            .ShouldBeTrue();

    [Fact] void should_resolve_the_same_scopes_every_time() =>
        DocumentScopes.Of(given.an_application.Build(), given.an_application.Options())
            .SequenceEqual(_scopes)
            .ShouldBeTrue();

    [Fact] void should_declare_the_module_document_within_the_module_itself() =>
        ScopeOf(given.an_application.Module).ModuleName.ShouldEqual("Accounting");

    [Fact] void should_declare_the_feature_document_within_the_module_holding_it() =>
        ScopeOf(given.an_application.Feature).ModuleName.ShouldEqual("Accounting");

    [Fact] void should_declare_the_rooted_feature_document_within_the_application_itself() =>
        ScopeOf(given.an_application.RootedFeature).ModuleName.ShouldEqual("Library");
}
