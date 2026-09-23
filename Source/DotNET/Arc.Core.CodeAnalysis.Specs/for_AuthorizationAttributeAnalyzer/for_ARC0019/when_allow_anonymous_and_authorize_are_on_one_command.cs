// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0019;

public class when_allow_anonymous_and_authorize_are_on_one_command
{
    [Fact] async Task should_report_the_conflict_at_allow_anonymous() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    [Authorize]
    [{|#0:AllowAnonymous|}]
    public record OpenAccount(string Name)
    {
        public void Handle() { }
    }
}",
        VerifyCS.Diagnostic("ARC0019")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("OpenAccount", "Authorize"));
}
