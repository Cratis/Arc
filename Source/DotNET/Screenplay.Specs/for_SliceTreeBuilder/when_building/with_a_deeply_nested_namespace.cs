// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_SliceTreeBuilder.when_building;

/// <summary>
/// Features nest to whatever depth the namespaces do, and the last segment is always the slice rather than a feature
/// wrapping a single slice.
/// </summary>
public class with_a_deeply_nested_namespace : Specification
{
    ScreenplayEmitter _emitter;
    ApplicationModel _model;
    ModuleSyntax _module;

    void Establish()
    {
        _emitter = new();
        _model = LibraryApplication.Build() with
        {
            Slices = [LibraryAuthors.Registration() with { Namespace = "Library.Authors.Onboarding.Registration" }]
        };
    }

    void Because() => _module = _emitter.Emit(_model, new ScreenplayOptions()).Application.Modules.Single();

    [Fact] void should_nest_the_outer_feature() => _module.Features.Select(_ => _.Name).ShouldContainOnly(["Authors"]);
    [Fact] void should_nest_the_inner_feature() => _module.Features.Single().Features.Select(_ => _.Name).ShouldContainOnly(["Onboarding"]);
    [Fact] void should_place_the_slice_in_the_inner_feature() => _module.Features.Single().Features.Single().Slices.Select(_ => _.Name).ShouldContainOnly(["Registration"]);
    [Fact] void should_not_place_the_slice_in_the_outer_feature() => _module.Features.Single().Slices.ShouldBeEmpty();
}
