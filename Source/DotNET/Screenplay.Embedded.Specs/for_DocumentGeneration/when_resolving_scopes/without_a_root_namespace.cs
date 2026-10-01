// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Generation;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_resolving_scopes;

/// <summary>
/// A project that never set <c>RootNamespace</c> still has one - the SDK gives it the assembly name - so the
/// hierarchy is resolved against that rather than against nothing. Asking the build for a property it already
/// defaults is how the two stay the same answer.
/// </summary>
public class without_a_root_namespace : Specification
{
    EmbeddedDocumentOptions _resolved;
    DocumentScope _root;

    void Because()
    {
        _resolved = new EmbeddedDocumentOptions("Library").Resolve();
        _root = DocumentScopes.Root(new EmbeddedDocumentOptions("Library"));
    }

    [Fact] void should_resolve_the_root_namespace_to_the_assembly_name() => _resolved.RootNamespace.ShouldEqual("Library");

    [Fact] void should_root_the_hierarchy_at_the_assembly_name() => _root.Id.ShouldEqual("Library");

    [Fact] void should_skip_no_segments_at_all() => _root.SegmentsToSkip.ShouldEqual(0);

    [Fact] void should_fall_back_to_the_neutral_name_when_nothing_names_the_assembly_either() =>
        new EmbeddedDocumentOptions(null).Resolve().RootNamespace.ShouldEqual(ScreenplayOptions.DefaultName);
}
