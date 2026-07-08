// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.CodeAnalysis.RolesLiteralAnalyzer, Cratis.Arc.CodeAnalysis.CodeFixes.UseNameofForRolesCodeFixProvider>;

namespace Cratis.Arc.CodeAnalysis.for_UseNameofForRolesCodeFixProvider;

public class when_rewriting_literal_to_nameof
{
    const string Source = @"
using Cratis.Arc.Authorization;

namespace TestNamespace
{
    public enum Role { Admin }

    [Roles({|#0:""Admin""|})]
    public class SomeController
    {
    }
}";

    const string FixedSource = @"
using Cratis.Arc.Authorization;

namespace TestNamespace
{
    public enum Role { Admin }

    [Roles(nameof(Role.Admin))]
    public class SomeController
    {
    }
}";

    [Fact] async Task should_rewrite_to_nameof() => await VerifyCS.VerifyCodeFixAsync(
        Source,
        FixedSource,
        new ExpectedDiagnostic("ARC0011", DiagnosticSeverity.Warning, "Admin"));
}
