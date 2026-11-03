// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_UnboundProductionAdmission;

public class when_attribution_is_not_safe : given.an_unbound_command
{
    ProtectedReadAdmitted _unlocated;
    ProtectedReadAdmitted _query;
    ProtectedReadAdmitted _withoutHandler;
    ProtectedReadAdmitted _ambiguous;
    string _withoutHandlerSource;
    string _ambiguousSource;

    void Because()
    {
        var verified = new ScreenplayVerifier().Verify(Candidate.Source);
        var verifier = Substitute.For<IScreenplayVerifier>();
        verifier.Verify(Candidate.Source).Returns(verified with { BindingDiagnostics = [Diagnostic.Error("PLAY9999", "An unknown error", SourceLocation.Start)] });
        _unlocated = new UnboundProductionAdmission(Emitter, verifier).Admit(Model, Options, Candidate);

        var queryDocument = new ScreenplayVerifier().VerifySyntax(Candidate.Source + "\n      query Missing => Unknown optional\n        by id Uuid\n");
        var query = queryDocument.Application!.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Queries).Single();
        verifier.Verify(Candidate.Source).Returns(queryDocument with { BindingDiagnostics = [Diagnostic.Error("PLAY0268", "Unknown query type", query.Location)] });
        _query = new UnboundProductionAdmission(Emitter, verifier).Admit(Model, Options, Candidate);

        var withoutHandler = Model with
        {
            Slices = Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with { SourceFilePath = null }).ToList()
            }).ToList()
        };
        var emission = Emitter.Emit(withoutHandler, Options);
        _withoutHandlerSource = emission.Source;
        _withoutHandler = new UnboundProductionAdmission(Emitter, Verifier).Admit(withoutHandler, Options, emission);

        var ambiguous = Model with { Slices = Model.Slices.Concat(Model.Slices).ToList() };
        emission = Emitter.Emit(ambiguous, Options);
        _ambiguousSource = emission.Source;
        _ambiguous = new UnboundProductionAdmission(Emitter, Verifier).Admit(ambiguous, Options, emission);
    }

    [Fact] void should_preserve_an_error_without_a_location() => _unlocated.Emission.Source.ShouldEqual(Candidate.Source);
    [Fact] void should_not_claim_an_unlocated_loss() => _unlocated.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_a_query_error() => _query.Emission.Source.ShouldEqual(Candidate.Source);
    [Fact] void should_not_attribute_a_query_to_the_preceding_command() => _query.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_an_error_when_no_handler_file_is_known() => _withoutHandler.Emission.Source.ShouldEqual(_withoutHandlerSource);
    [Fact] void should_preserve_an_ambiguous_command_scope() => _ambiguous.Emission.Source.ShouldEqual(_ambiguousSource);
}
