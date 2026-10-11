// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_pii_and_scoped_secret : given.a_compliance_document
{
    void Because() => GenerateSecret("Namespace", authoring: true, pii: true);

    [Fact] void should_keep_both_markers() => Result.Source.ShouldContain("concept ApiKey : String pii secret");
    [Fact] void should_not_state_an_ignored_scope() => Result.Source.ShouldNotContain("secret scope");
    [Fact] void should_not_report_scope_loss() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EncryptionScopeNotRepresentable).ShouldBeEmpty();
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
