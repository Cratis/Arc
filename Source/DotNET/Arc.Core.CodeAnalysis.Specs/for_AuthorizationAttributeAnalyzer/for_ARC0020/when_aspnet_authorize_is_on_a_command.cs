// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.AuthorizationAttributeAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_AuthorizationAttributeAnalyzer.for_ARC0020;

public class when_aspnet_authorize_is_on_a_command
{
    [Fact] async Task should_report_that_it_is_not_enforced() => await VerifyCS.VerifyAnalyzerAsync(@"
namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizeAttribute : System.Attribute { }
    public class AllowAnonymousAttribute : System.Attribute { }
}

namespace TestNamespace
{
    using Cratis.Arc.Commands.ModelBound;
    using Microsoft.AspNetCore.Authorization;

    [Command]
    [{|#0:Authorize|}]
    public record CloseAccount(string Id)
    {
        public void Handle() { }
    }
}",
        VerifyCS.Diagnostic("ARC0020")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Authorize", "CloseAccount", "Authorize"));
}
