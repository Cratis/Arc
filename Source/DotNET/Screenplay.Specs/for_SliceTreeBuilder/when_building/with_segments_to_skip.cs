// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_SliceTreeBuilder.when_building;

/// <summary>
/// The namespace of a slice usually leads with segments that say nothing about the model - the company, the product,
/// the assembly. Skipping them is what keeps the feature tree about the domain.
/// </summary>
public class with_segments_to_skip : Specification
{
    ScreenplayEmitter _emitter;
    ApplicationModel _model;
    ModuleSyntax _module;

    void Establish()
    {
        _emitter = new();
        _model = LibraryApplication.Build() with
        {
            Slices = [LibraryAuthors.Registration() with { Namespace = "Contoso.Library.Authors.Registration" }]
        };
    }

    void Because() => _module = _emitter
        .Emit(_model, new ScreenplayOptions { SegmentsToSkip = 1 })
        .Application.Modules.Single();

    [Fact] void should_drop_the_skipped_segment() => _module.Features.Select(_ => _.Name).ShouldContainOnly(["Authors"]);
    [Fact] void should_keep_the_slice_under_the_feature() => _module.Features.Single().Slices.Select(_ => _.Name).ShouldContainOnly(["Registration"]);
}
