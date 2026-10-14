// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Library;

namespace Cratis.Arc.Screenplay.for_ScreenReferenceScopes.when_qualifying;

public class with_unique_module_roots : given.an_application_with_screen_references
{
    void Establish()
    {
        Model = Model with { Slices = [.. Model.Slices, LibraryAuthors.Registration() with { Namespace = "Store.Authors.Registration" }] };
        Options = new() { ModulesFromNamespaceRoots = true };
    }

    void Because() => Emit();

    [Fact] void should_use_the_provably_unique_full_scope() => Emission.Source.Contains("action Library.Authors.Registration.RegisterAuthor", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_qualify_the_toolbar_with_the_same_scope() => Emission.Source.Contains("item registerAuthor action Library.Authors.Registration.RegisterAuthor", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_only_the_missing_query() => Emission.Diagnostics.Count(_ => _.Code == "SP0028").ShouldEqual(1);
}
