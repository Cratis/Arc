// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Library;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayEmitter.when_emitting;

/// <summary>
/// A host emitting a model it already has configures nothing, and there is no compilation to name the document
/// after. The domain the model carries is the only answer available, so the emitter is where the options an emission
/// runs with are resolved - moving that any deeper meant it happened twice on the way through a generation.
/// </summary>
public class a_model_with_nothing_configured_alongside_it : given.an_emitter
{
    ApplicationModel _model;
    ScreenplayEmission _emission;

    void Establish() => _model = LibraryApplication.Build() with { Domain = "Lending", Module = string.Empty };

    void Because() => _emission = _emitter.Emit(_model, new ScreenplayOptions());

    [Fact] void should_name_the_document_after_the_domain_the_model_carries() => _emission.Source.StartsWith("domain Lending", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_fall_back_to_the_domain_for_the_module() => _emission.Source.Contains("module Lending", StringComparison.Ordinal).ShouldBeTrue();
}
