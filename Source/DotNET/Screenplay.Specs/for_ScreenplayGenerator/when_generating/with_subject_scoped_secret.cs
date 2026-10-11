// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class with_subject_scoped_secret : given.a_compliance_document
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_subject_scope_implicit_without_a_loss_diagnostic(bool authoring)
    {
        GenerateSecret("Subject", authoring);

        Result.Source.ShouldContain("concept ApiKey : String secret");
        Result.Source.ShouldNotContain("secret scope");
        Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.EncryptionScopeNotRepresentable).ShouldBeEmpty();
        AssertComplianceDocument();
    }
}
