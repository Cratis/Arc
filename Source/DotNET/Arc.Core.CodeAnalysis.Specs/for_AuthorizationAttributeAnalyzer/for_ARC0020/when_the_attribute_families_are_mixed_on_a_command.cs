// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0020;

public class when_the_attribute_families_are_mixed_on_a_command
{
    [Fact] async Task should_report_both_the_conflict_and_the_ignored_attribute() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizeAttribute : System.Attribute { }
    public class AllowAnonymousAttribute : System.Attribute { }
}

namespace TestNamespace
{
    using Cratis.Arc.Commands.ModelBound;

    [Command]
    [Cratis.Arc.Authorization.Authorize]
    [{|#0:Microsoft.AspNetCore.Authorization.AllowAnonymous|}]
    public record CloseAccount(string Id)
    {
        public void Handle() { }
    }
}",
        VerifyCS.Diagnostic("ARC0019")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("CloseAccount", "Authorize"),
        VerifyCS.Diagnostic("ARC0020")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("AllowAnonymous", "CloseAccount", "AllowAnonymous"));
}
