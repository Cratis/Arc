// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.RolesLiteralAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_RolesLiteralAnalyzer.for_ARC0011;

public class when_roles_uses_string_literal
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Authorization;

namespace TestNamespace
{
    [Roles({|#0:""Admin""|})]
    public class SomeController
    {
    }
}",
        VerifyCS.Diagnostic("ARC0011")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Admin"));
}
