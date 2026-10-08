// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerifier.when_verifying_syntax;

public class with_a_syntax_error : Specification
{
    ScreenplayVerification _result;

    void Because() => _result = new ScreenplayVerifier().VerifySyntax("module Library\n  not a feature");

    [Fact] void should_reject_the_syntax() => _result.Compiles.ShouldBeFalse();
    [Fact] void should_report_the_syntax_error() => _result.Errors.ShouldNotBeEmpty();
    [Fact] void should_not_attempt_binding() => _result.BindingDiagnostics.ShouldBeEmpty();
}
