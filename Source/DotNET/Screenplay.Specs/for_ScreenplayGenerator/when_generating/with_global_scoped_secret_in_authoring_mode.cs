// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_global_scoped_secret_in_authoring_mode : given.a_compliance_document
{
    void Because() => GenerateSecret("Global", authoring: true);

    [Fact] void should_state_the_scope() => Result.Source.ShouldContain("secret scope global");
    [Fact] void should_not_report_scope_loss() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EncryptionScopeNotRepresentable).ShouldBeEmpty();
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
