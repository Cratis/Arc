// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_UnboundProductionAdmission;

public class when_a_command_does_not_bind : given.an_unbound_command
{
    Analysis.ProtectedReadAdmitted _authoring;

    void Because()
    {
        Admit();
        var authoring = Options with { AuthoringOnlyConstructs = true };
        _authoring = new Analysis.UnboundProductionAdmission(Emitter, Verifier).Admit(Model, authoring, Emitter.Emit(Model, authoring));
    }

    [Fact] void should_start_with_a_real_operand_error() => new ScreenplayVerifier().Verify(Candidate.Source).UnexpectedBindingErrors().Select(error => error.Code).ShouldContain("PLAY0268");
    [Fact] void should_leave_the_handler_in_code() => Admitted.Emission.Source.ShouldContain("handler\n");
    [Fact] void should_withhold_the_invalid_condition() => Admitted.Emission.Source.ShouldNotContain("role == 2");
    [Fact] void should_keep_other_commands() => Admitted.Emission.Source.ShouldContain("command Keep");
    [Fact] void should_keep_unrelated_productions() => Admitted.Emission.Source.ShouldContain("Kept");
    [Fact] void should_report_the_original_binder_error() => Admitted.Diagnostics.Single().Message.ShouldContain("PLAY0268");
    [Fact] void should_report_the_loss_with_sp0056() => Admitted.Diagnostics.Single().Code.ShouldEqual(ScreenplayDiagnosticCodes.DocumentDidNotBind);
    [Fact] void should_report_the_loss_as_a_warning() => Admitted.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_return_a_document_that_binds() => new ScreenplayVerifier().Verify(Admitted.Emission.Source).UnexpectedBindingErrors().ShouldBeEmpty();

    [Fact]
    void should_also_withhold_in_authoring_mode()
    {
        _authoring.Emission.Source.ShouldNotContain("role == 2");
        new ScreenplayVerifier().Verify(_authoring.Emission.Source).UnexpectedBindingErrors(true).ShouldBeEmpty();
    }
}
