// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class and_a_condition_has_a_mismatched_enumeration_operand : given.a_printed_document
{
    const string Source = """
        concept Role : Enum
          reader
          administrator
        module Library
          feature Authors
            slice StateChange Registration
              command Register
                id Uuid identifier
                role Role
                produces when role == 2
                  Registered
                    for id
                    role = role
              event Registered
                role Role
        """;

    void Because() => Generate(Source);

    [Fact] void should_compile_the_printed_document() => Verified.Compiles.ShouldBeTrue();
    [Fact] void should_preserve_the_play0268_operand_error_for_the_end_to_end_gate() => Verified.UnexpectedBindingErrors().Single().Code.ShouldEqual("PLAY0268");
    [Fact] void should_identify_the_incompatible_operand() => Verified.UnexpectedBindingErrors().Single().Message.ShouldEqual("Condition operand for 'role' must match its scalar type and declared enumeration values.");
    [Fact] void should_report_sp0056() => Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldEqual(1);
    [Fact] void should_preserve_the_operand_error_in_sp0056() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).Message.ShouldContain(Verified.UnexpectedBindingErrors().Single().Message);

    [Fact]
    void should_not_hide_the_error_in_authoring_mode()
    {
        Generate(Source, authoringOnlyConstructs: true);
        Verified.UnexpectedBindingErrors(true).Single().Code.ShouldEqual("PLAY0268");
        Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldEqual(1);
    }
}
