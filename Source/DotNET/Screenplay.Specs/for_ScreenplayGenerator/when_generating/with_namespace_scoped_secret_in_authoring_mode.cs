// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_namespace_scoped_secret_in_authoring_mode : given.a_compliance_document
{
    void Because() => GenerateSecret("Namespace", authoring: true);

    [Fact] void should_state_the_scope() => Result.Source.ShouldContain("secret scope namespace");
    [Fact] void should_preserve_the_scope_in_the_syntax() => new Cratis.Screenplay.ScreenplayCompiler().Compile(Result.Source).Value!.Concepts.Single().Attributes.Single().Scope.ShouldEqual("namespace");
    [Fact] void should_not_report_scope_loss() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EncryptionScopeNotRepresentable).ShouldBeEmpty();
    [Fact] void should_compile_and_round_trip() => AssertComplianceDocument();
}
