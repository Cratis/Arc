// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerification.when_classifying_binding_errors;

public class with_a_moved_attachment_refusal : Specification
{
    const string Unextended = "Command 'Register' handler requires a constrained implementation attachment.";

    [Fact] void should_not_exempt_the_old_text_under_the_old_code() => AssertNotExpected("PLAY0268", Unextended);

    [Fact]
    void should_not_exempt_the_suffixed_text_of_a_handler_with_an_unresolved_body() => AssertNotExpected(
        "PLAY0800",
        "Command 'Register' handler requires a constrained implementation attachment: the handler body is empty or missing. Declare an unconditional 'produces <Event>' beside a resolved, nonempty, language-tagged handler body; split conditional or variable outcomes into separate commands, or move them to declarative 'produces ... when' without a body.");

    [Fact]
    void should_not_exempt_the_suffixed_text_of_a_conditional_production() => AssertNotExpected(
        "PLAY0800",
        "Command 'Register' handler requires a constrained implementation attachment: a conditional 'produces ... when' cannot sit beside a handler body. Declare an unconditional 'produces <Event>' beside the handler body, split conditional or variable outcomes into separate commands, or move them to declarative 'produces ... when' without a body.");

    [Fact] void should_not_exempt_the_unresolved_implementation_requirement_text() => AssertNotExpected("PLAY0800", "Command 'Register' has an attached handler whose implementation requirement 'r' must resolve to a non-empty, language-tagged body.");

    [Fact] void should_not_exempt_the_text_under_another_code() => AssertNotExpected("PLAY0271", Unextended);

    [Fact]
    void should_exempt_the_unextended_text_under_the_new_code()
    {
        var verified = WithError("PLAY0800", Unextended);
        verified.UnexpectedBindingErrors().ShouldBeEmpty();
        verified.UnexpectedBindingErrors(true).ShouldBeEmpty();
    }

    static void AssertNotExpected(string code, string message)
    {
        var verified = WithError(code, message);
        verified.UnexpectedBindingErrors().Count.ShouldEqual(1);
        verified.UnexpectedBindingErrors(true).Count.ShouldEqual(1);
    }

    static ScreenplayVerification WithError(string code, string message) => new(string.Empty, null, [])
    {
        BindingDiagnostics = [Diagnostic.Error(code, message, SourceLocation.Start)]
    };
}
