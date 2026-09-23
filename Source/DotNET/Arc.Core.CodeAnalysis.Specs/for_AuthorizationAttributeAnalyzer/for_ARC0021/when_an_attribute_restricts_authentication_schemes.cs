// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0021;

public class when_an_attribute_restricts_authentication_schemes
{
    [Fact] async Task should_report_that_the_schemes_are_not_evaluated() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    [{|#0:Authorize(Roles = ""Admin"", AuthenticationSchemes = ""Bearer"")|}]
    public record UpdateProfile(string Name)
    {
        public void Handle() { }
    }
}",
        VerifyCS.Diagnostic("ARC0021")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Authorize", "UpdateProfile", "AuthenticationSchemes"));
}
