// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_global_scoped_secret : given.a_compliance_document
{
    void Because() => GenerateSecret("Global");

    [Fact] void should_keep_the_secret_marker() => Result.Source.ShouldContain("concept ApiKey : String secret");
    [Fact] void should_not_state_authoring_scope() => Result.Source.ShouldNotContain("secret scope");
    [Fact] void should_report_the_scope_loss() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EncryptionScopeNotRepresentable).Message.ShouldContain("Concept 'ApiKey' has encryption scope 'global', which is not stated; bare secret implies Subject scope");
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
