// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_UnboundProductionAdmission;

public class when_the_document_already_verifies : given.an_unbound_command
{
    IScreenplayEmitter _emitter;

    void Establish()
    {
        Model = Model with
        {
            Slices = Model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => command with
                {
                    Produces = command.Produces.Select(production => production with { When = null }).ToList()
                }).ToList()
            }).ToList()
        };
        Candidate = Emitter.Emit(Model, Options);
        _emitter = Substitute.For<IScreenplayEmitter>();
    }

    void Because() => Admitted = new UnboundProductionAdmission(_emitter, Verifier).Admit(Model, Options, Candidate);

    [Fact] void should_start_with_a_verified_document() => new ScreenplayVerifier().Verify(Candidate.Source).UnexpectedBindingErrors().ShouldBeEmpty();
    [Fact] void should_preserve_the_document_byte_for_byte() => Admitted.Emission.Source.ShouldEqual(Candidate.Source);
    [Fact] void should_not_report_a_loss() => Admitted.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_reemit_the_document() => _emitter.DidNotReceive().Emit(Arg.Any<Model.ApplicationModel>(), Arg.Any<ScreenplayOptions>());
}
